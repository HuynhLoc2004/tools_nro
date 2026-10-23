using System;
using System.Collections.Generic;
using Assets.src.g;
using UnityEngine;

public class BossNotice
{
	public string bossName = "";
	public string mapName = "";
	public int mapId = -1;
	public DateTime appearTime;
	public int hitX;
	public int hitY;
	public int hitW;
	public int hitH;
}

public class MapTargetInfo
{
	public bool isBoss;
	public string bossName = "";
	public string playerName = "";
	public string planet = "TD"; // TD, NM, XD
	public string clan = "";
	public long hp;
	public long hpFull;
	public int cx;
	public int cy;
	public Char charRef;
	public Mob mobRef;
	public int hitX;
	public int hitY;
	public int hitW;
	public int hitH;
}

public class BossManager
{
	public static bool isShowBossNotice = true;
	public static bool isShowCharInMap = true;

	public static List<BossNotice> listBossNotices = new List<BossNotice>();
	public static List<MapTargetInfo> currentMapTargets = new List<MapTargetInfo>();

	public static MapTargetInfo lastClickedTarget = null;
	private static long lastClickTargetTime = 0L;

	public static void ClearHitBoxes()
	{
		if (currentMapTargets != null)
		{
			for (int i = 0; i < currentMapTargets.Count; i++)
			{
				MapTargetInfo target = currentMapTargets[i];
				if (target != null)
				{
					target.hitX = -1000;
					target.hitY = -1000;
					target.hitW = 0;
					target.hitH = 0;
				}
			}
		}

		if (listBossNotices != null)
		{
			lock (listBossNotices)
			{
				for (int i = 0; i < listBossNotices.Count; i++)
				{
					BossNotice notice = listBossNotices[i];
					if (notice != null)
					{
						notice.hitX = -1000;
						notice.hitY = -1000;
						notice.hitW = 0;
						notice.hitH = 0;
					}
				}
			}
		}
	}

	public static void ToggleBossNotice()
	{
		isShowBossNotice = !isShowBossNotice;
		Rms.saveRMSInt("show_boss_notice", isShowBossNotice ? 1 : 0);
		GameScr.info1.addInfo("Thông báo Boss: " + (isShowBossNotice ? "BẬT" : "TẮT"), 0);
	}

	public static void ToggleCharInMap()
	{
		isShowCharInMap = !isShowCharInMap;
		Rms.saveRMSInt("show_char_in_map", isShowCharInMap ? 1 : 0);
		GameScr.info1.addInfo("Hiện người trong khu: " + (isShowCharInMap ? "BẬT" : "TẮT"), 0);
	}

	public static void LoadSettings()
	{
		int sbn = Rms.loadRMSInt("show_boss_notice");
		if (sbn != -1) isShowBossNotice = (sbn == 1);

		int scm = Rms.loadRMSInt("show_char_in_map");
		if (scm != -1) isShowCharInMap = (scm == 1);
	}

	/// <summary>
	/// Thêm thông báo boss xuất hiện từ server chat (chatVip).
	/// Giữ tối đa 5 thông báo mới nhất (FIFO: đẩy thông báo cũ ở đầu mất đi).
	/// </summary>
	public static void AddBossNotice(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		string raw = text.Trim();
		if (raw.StartsWith("!"))
		{
			raw = raw.Substring(1).Trim();
		}

		bool isBossNotice = raw.StartsWith("BOSS ", StringComparison.OrdinalIgnoreCase)
			|| raw.StartsWith("[BOSS]", StringComparison.OrdinalIgnoreCase)
			|| raw.StartsWith("Boss: ", StringComparison.OrdinalIgnoreCase)
			|| raw.Contains(" vừa xuất hiện tại ")
			|| raw.Contains(" appear at ");

		if (!isBossNotice)
		{
			return;
		}

		string s = raw;
		if (s.StartsWith("BOSS ", StringComparison.OrdinalIgnoreCase))
		{
			s = s.Substring(5).Trim();
		}
		else if (s.StartsWith("[BOSS]", StringComparison.OrdinalIgnoreCase))
		{
			s = s.Substring(6).Trim();
		}
		else if (s.StartsWith("Boss: ", StringComparison.OrdinalIgnoreCase))
		{
			s = s.Substring(6).Trim();
		}
		else if (s.StartsWith("Boss ", StringComparison.OrdinalIgnoreCase))
		{
			s = s.Substring(5).Trim();
		}

		string nameBoss = "";
		string mapName = "";

		if (s.Contains(" vừa xuất hiện tại "))
		{
			string[] parts = s.Split(new string[] { " vừa xuất hiện tại " }, StringSplitOptions.None);
			nameBoss = parts[0].Trim();
			mapName = parts[1].Trim();
		}
		else if (s.Contains(" appear at "))
		{
			string[] parts = s.Split(new string[] { " appear at " }, StringSplitOptions.None);
			nameBoss = parts[0].Trim();
			mapName = parts[1].Trim();
		}
		else
		{
			return;
		}

		if (mapName.EndsWith("."))
		{
			mapName = mapName.Substring(0, mapName.Length - 1).Trim();
		}

		// Tìm mapId từ tên map
		int mapId = XmapManager.findMapIdByName(mapName);
		if (mapId < 0 && TileMap.mapNames != null)
		{
			for (int i = 0; i < TileMap.mapNames.Length; i++)
			{
				if (TileMap.mapNames[i] != null && TileMap.mapNames[i].Equals(mapName, StringComparison.OrdinalIgnoreCase))
				{
					mapId = i;
					break;
				}
			}
		}
		if ((nameBoss.ToLower().Contains("tiểu đội") || nameBoss.ToLower().Contains("số ")) && mapName.Contains("Trạm tàu vũ trụ"))
		{
			mapId = 25;
		}

		BossNotice notice = new BossNotice
		{
			bossName = nameBoss,
			mapName = mapName,
			mapId = mapId,
			appearTime = DateTime.Now
		};

		lock (listBossNotices)
		{
			listBossNotices.Add(notice);
			while (listBossNotices.Count > 5)
			{
				listBossNotices.RemoveAt(0);
			}
		}
	}

	/// <summary>
	/// Kiểm tra xem nhân vật có phải là Boss không.
	/// </summary>
	public static bool IsBoss(Char c)
	{
		if (c == null || string.IsNullOrEmpty(c.cName))
		{
			return false;
		}
		if (c.isPet || c.isMiniPet || c == Char.myCharz())
		{
			return false;
		}
		if (c.cName == "Trọng tài" || c.cName.StartsWith("#") || c.cName.StartsWith("$"))
		{
			return false;
		}

		// Trong NRO: Boss có charID < 0 hoặc cTypePk == 5
		if (c.charID < 0 || c.cTypePk == 5)
		{
			return true;
		}

		// Kiểm tra trùng tên với thông báo boss
		lock (listBossNotices)
		{
			for (int i = 0; i < listBossNotices.Count; i++)
			{
				if (listBossNotices[i] != null && !string.IsNullOrEmpty(listBossNotices[i].bossName))
				{
					if (c.cName.Equals(listBossNotices[i].bossName, StringComparison.OrdinalIgnoreCase) ||
						c.cName.Contains(listBossNotices[i].bossName) ||
						listBossNotices[i].bossName.Contains(c.cName))
					{
						return true;
					}
				}
			}
		}

		// Tên có viết hoa chữ cái đầu và dài hơn bình thường
		if (c.cName.Length > 3 && char.IsUpper(c.cName[0]) && (c.cName.Contains(" ") || c.cHPFull > 200000000L))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Kiểm tra quái có phải quái Boss không (BigBoss, BachTuoc, v.v.).
	/// </summary>
	public static bool IsBossMob(Mob m)
	{
		if (m == null || m.hp <= 0 || m.status == 0)
		{
			return false;
		}
		if (m is BigBoss || m is BigBoss2 || m is BachTuoc)
		{
			return true;
		}
		if (m.getTemplate() != null && !string.IsNullOrEmpty(m.getTemplate().name))
		{
			string tName = m.getTemplate().name;
			lock (listBossNotices)
			{
				for (int i = 0; i < listBossNotices.Count; i++)
				{
					if (listBossNotices[i] != null && !string.IsNullOrEmpty(listBossNotices[i].bossName))
					{
						if (tName.Equals(listBossNotices[i].bossName, StringComparison.OrdinalIgnoreCase) ||
							tName.Contains(listBossNotices[i].bossName))
						{
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Rút gọn số lượng lớn (k, Tr, T).
	/// Ví dụ: 1.000.000 -> 1Tr; 1.200.000 -> 1,2Tr; 50.000.000.000 -> 50T
	/// </summary>
	public static string FormatShortMoney(long m)
	{
		if (m >= 1000000000L)
		{
			double d = (double)m / 1000000000.0;
			return (d % 1 == 0) ? $"{d:0}T" : $"{d:0.0}T".Replace(".", ",");
		}
		if (m >= 1000000L)
		{
			double d = (double)m / 1000000.0;
			return (d % 1 == 0) ? $"{d:0}Tr" : $"{d:0.0}Tr".Replace(".", ",");
		}
		if (m >= 1000L)
		{
			double d = (double)m / 1000.0;
			return (d % 1 == 0) ? $"{d:0}k" : $"{d:0.0}k".Replace(".", ",");
		}
		return m.ToString();
	}

	/// <summary>
	/// Cập nhật real-time danh sách Boss và Người chơi trong map hiện tại.
	/// </summary>
	public static void Update()
	{
		// Chặn click mục tiêu nếu đang mở dialog/menu/panel/chat hoặc vừa đóng dialog (< 400ms)
		if (ModManager.isAnyDialogOpen || (GameCanvas.panel != null && GameCanvas.panel.isShow) || (GameCanvas.menu != null && GameCanvas.menu.showMenu) || (ChatTextField.gI() != null && ChatTextField.gI().isShow) || (mSystem.currentTimeMillis() - ModManager.lastDialogClickTime < 400L))
		{
			return;
		}

		// Cập nhật click chuột trái để tương tác nếu có
		if (GameCanvas.isPointerJustRelease || Input.GetMouseButtonUp(0))
		{
			HandleLeftClick(GameCanvas.px, GameCanvas.py);
		}
	}

	/// <summary>
	/// Vẽ toàn bộ hệ thống Boss:
	/// 1. Sợi chỉ vàng nối từ nhân vật tới boss trong map.
	/// 2. Thông báo boss ra (tối đa 5 dòng, vàng nếu khác map, đỏ nếu cùng map).
	/// 3. Danh sách thông tin Boss và Người trong map (tối đa 10 dòng).
	/// </summary>
	public static void Paint(mGraphics g)
	{
		if (GameCanvas.currentScreen is not GameScr || Char.myCharz() == null)
		{
			return;
		}

		// Nếu đang mở bất kỳ popup/dialog nào (Mod Menu X, Panel hành trang/kỹ năng, Menu NPC, Chat)
		// thì tạm ẩn thông báo và danh sách mục tiêu phía sau bảng để tránh rối mắt và chống tuyệt đối click nhầm
		if (ModManager.isAnyDialogOpen || (GameCanvas.panel != null && GameCanvas.panel.isShow) || (GameCanvas.menu != null && GameCanvas.menu.showMenu))
		{
			ClearHitBoxes();
			return;
		}

		// 1. SỢI CHỈ MÀU VÀNG NỐI TỚI BOSS TRONG MAP (hình 5)
		PaintBossLaser(g);

		// 2. THÔNG BÁO BOSS RA Ở GÓC TRÊN BÊN PHẢI (dưới nút chat, tối đa 5 thông báo)
		int curY = 40;
		int margin = 5;

		// 2. THÔNG BÁO BOSS RA Ở GÓC TRÊN BÊN PHẢI (CHỈ HIỆN KHI BẬT isShowBossNotice)
		if (isShowBossNotice)
		{
			lock (listBossNotices)
			{
				try
				{
					mFont.customFontSize = 11; // Kích thước chữ nhỏ gọn chuẩn Word 8-9
					for (int i = 0; i < listBossNotices.Count; i++)
					{
						BossNotice notice = listBossNotices[i];
						if (notice == null) continue;

						TimeSpan span = DateTime.Now.Subtract(notice.appearTime);
						int totalSec = (int)span.TotalSeconds;
						string timeStr = (totalSec < 60) ? $"{totalSec}s" : $"{totalSec / 60}p{totalSec % 60}s";

						string noticeText = $"{notice.bossName} - {notice.mapName} - {timeStr}";

						bool isInSameMap = (TileMap.mapID == notice.mapId) ||
							(!string.IsNullOrEmpty(TileMap.mapName) && TileMap.mapName.Equals(notice.mapName, StringComparison.OrdinalIgnoreCase));

						mFont fontText = isInSameMap ? mFont.tahoma_7_red : mFont.tahoma_7_yellow;

						int lineW = fontText.getWidth(noticeText);
						int lineX = GameCanvas.w - lineW - margin;

						fontText.drawString(g, noticeText, lineX, curY, 0, mFont.tahoma_7b_dark);

						notice.hitX = -1000;
						notice.hitY = -1000;
						notice.hitW = 0;
						notice.hitH = 0;

						curY += 8;
					}
				}
				finally
				{
					mFont.customFontSize = 0;
				}
			}
		}

		// 3. DANH SÁCH USER VÀ BOSS TRONG MAP HIỆN TẠI (CHỈ HIỆN KHI BẬT isShowCharInMap)
		if (isShowCharInMap)
		{
			UpdateCurrentMapTargets();

			if (currentMapTargets.Count > 0)
			{
				if (isShowBossNotice && listBossNotices.Count > 0)
				{
					curY += 2; // Khoảng cách nhỏ giữa thông báo và danh sách người/boss
				}

				try
				{
					// Thiết lập kích thước font nhỏ gọn chuẩn Word size 8-9 (11px)
					mFont.customFontSize = 11;

					// Tính độ rộng lớn nhất để căn lề đẹp mắt
					int maxW = 80;
					for (int i = 0; i < currentMapTargets.Count; i++)
					{
						MapTargetInfo target = currentMapTargets[i];
						int w = GetTargetLineWidth(i + 1, target);
						if (w > maxW) maxW = w;
					}

					int listX = GameCanvas.w - maxW - margin;

					for (int i = 0; i < currentMapTargets.Count; i++)
					{
						MapTargetInfo target = currentMapTargets[i];
						int stt = i + 1;

						int drawX = listX;
						if (target.isBoss)
						{
							// Boss: STT (cyan), Tên Boss (yellow), HP (red) - Font nhỏ gọn
							string sttStr = $"{stt}. ";
							string nameStr = $"{target.bossName} ";
							string hpStr = $"[{NinjaUtil.getMoneys(target.hp)}/{FormatShortMoney(target.hpFull)}]";

							mFont.tahoma_7_blue1.drawString(g, sttStr, drawX, curY, 0, mFont.tahoma_7b_dark);
							drawX += mFont.tahoma_7_blue1.getWidth(sttStr);

							mFont.tahoma_7_yellow.drawString(g, nameStr, drawX, curY, 0, mFont.tahoma_7b_dark);
							drawX += mFont.tahoma_7_yellow.getWidth(nameStr);

							mFont.tahoma_7_red.drawString(g, hpStr, drawX, curY, 0, mFont.tahoma_7b_dark);
						}
						else
						{
							// Người chơi: STT. Hành Tinh:[tên pt] (cyan), Tên nhân vật (white), HP (red) - Font nhỏ gọn
							string clanStr = !string.IsNullOrEmpty(target.clan) ? $"[{target.clan}] " : "";
							string prefixStr = $"{stt}. {target.planet}:{clanStr}";
							string nameStr = $"{target.playerName} ";
							string hpStr = $"[{NinjaUtil.getMoneys(target.hp)}/{FormatShortMoney(target.hpFull)}]";

							mFont.tahoma_7_blue1.drawString(g, prefixStr, drawX, curY, 0, mFont.tahoma_7b_dark);
							drawX += mFont.tahoma_7_blue1.getWidth(prefixStr);

							mFont.tahoma_7_white.drawString(g, nameStr, drawX, curY, 0, mFont.tahoma_7b_dark);
							drawX += mFont.tahoma_7_white.getWidth(nameStr);

							mFont.tahoma_7_red.drawString(g, hpStr, drawX, curY, 0, mFont.tahoma_7b_dark);
						}

						target.hitX = listX;
						target.hitY = curY;
						target.hitW = maxW;
						target.hitH = 8;

						curY += 8;
					}
				}
				finally
				{
					mFont.customFontSize = 0;
				}
			}
		}
	}

	private static int GetTargetLineWidth(int stt, MapTargetInfo target)
	{
		if (target.isBoss)
		{
			string sttStr = $"{stt}. ";
			string nameStr = $"{target.bossName} ";
			string hpStr = $"[{NinjaUtil.getMoneys(target.hp)}/{FormatShortMoney(target.hpFull)}]";
			return mFont.tahoma_7_blue1.getWidth(sttStr) + mFont.tahoma_7_yellow.getWidth(nameStr) + mFont.tahoma_7_red.getWidth(hpStr);
		}
		else
		{
			string clanStr = !string.IsNullOrEmpty(target.clan) ? $"[{target.clan}] " : "";
			string prefixStr = $"{stt}. {target.planet}:{clanStr}";
			string nameStr = $"{target.playerName} ";
			string hpStr = $"[{NinjaUtil.getMoneys(target.hp)}/{FormatShortMoney(target.hpFull)}]";
			return mFont.tahoma_7_blue1.getWidth(prefixStr) + mFont.tahoma_7_white.getWidth(nameStr) + mFont.tahoma_7_red.getWidth(hpStr);
		}
	}

	/// <summary>
	/// Vẽ tia chỉ vàng nối từ nhân vật tới boss trong khu.
	/// </summary>
	private static void PaintBossLaser(mGraphics g)
	{
		Char my = Char.myCharz();
		if (my == null || Char.isLoadingMap) return;

		int myScreenX = my.cx - GameScr.cmx;
		int myScreenY = my.cy - GameScr.cmy;

		if (GameScr.vCharInMap != null)
		{
			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char c = (Char)GameScr.vCharInMap.elementAt(i);
				if (c != null && IsBoss(c) && c.cHP > 0 && c.statusMe != 14)
				{
					int bossScreenX = c.cx - GameScr.cmx;
					int bossScreenY = c.cy - GameScr.cmy;

					g.setColor(16776960); // Màu vàng tươi (0xFFFF00)
					g.drawLine(myScreenX, myScreenY, bossScreenX, bossScreenY);
				}
			}
		}

		if (GameScr.vMob != null)
		{
			for (int i = 0; i < GameScr.vMob.size(); i++)
			{
				Mob m = (Mob)GameScr.vMob.elementAt(i);
				if (m != null && IsBossMob(m) && m.hp > 0 && m.status != 0)
				{
					int mobScreenX = m.x - GameScr.cmx;
					int mobScreenY = m.y - GameScr.cmy;

					g.setColor(16776960); // Màu vàng tươi (0xFFFF00)
					g.drawLine(myScreenX, myScreenY, mobScreenX, mobScreenY);
				}
			}
		}
	}

	private static long lastMapTargetsUpdateTime = 0L;

	/// <summary>
	/// Quét và làm mới danh sách Boss và Người chơi trong map hiện tại theo thứ tự:
	/// Boss ở trên cùng, sau đó tới người chơi. Tối đa 10 dòng.
	/// </summary>
	private static void UpdateCurrentMapTargets()
	{
		long now = mSystem.currentTimeMillis();
		// Throttle 250ms: Không quét lại mỗi frame của render GUI để tránh tạo rác GC và drop FPS
		if (now - lastMapTargetsUpdateTime < 250L && currentMapTargets.Count > 0)
		{
			return;
		}
		lastMapTargetsUpdateTime = now;

		List<MapTargetInfo> bosses = new List<MapTargetInfo>();
		List<MapTargetInfo> players = new List<MapTargetInfo>();

		// 1. Quét Boss từ vMob
		if (GameScr.vMob != null)
		{
			for (int i = 0; i < GameScr.vMob.size(); i++)
			{
				Mob m = (Mob)GameScr.vMob.elementAt(i);
				if (m != null && IsBossMob(m) && m.hp > 0 && m.status != 0)
				{
					MapTargetInfo info = new MapTargetInfo
					{
						isBoss = true,
						bossName = (m.getTemplate() != null) ? m.getTemplate().name : "Boss",
						hp = m.hp,
						hpFull = (m.maxHp > 0) ? m.maxHp : m.hp,
						cx = m.x,
						cy = m.y,
						mobRef = m
					};
					bosses.Add(info);
				}
			}
		}

		// 2. Quét Char từ vCharInMap
		if (GameScr.vCharInMap != null)
		{
			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char c = (Char)GameScr.vCharInMap.elementAt(i);
				if (c == null || c == Char.myCharz()) continue;
				if (string.IsNullOrEmpty(c.cName) || c.cName == "Trọng tài") continue;
				if (c.isPet || c.isMiniPet || c.cName.StartsWith("#") || c.cName.StartsWith("$")) continue;
				if (c.statusMe == 14) continue; // Bị chết/biến mất

				if (IsBoss(c))
				{
					MapTargetInfo info = new MapTargetInfo
					{
						isBoss = true,
						bossName = c.cName,
						hp = c.cHP,
						hpFull = (c.cHPFull > 0) ? c.cHPFull : c.cHP,
						cx = c.cx,
						cy = c.cy,
						charRef = c
					};
					bosses.Add(info);
				}
				else
				{
					string planet = (c.cgender == 0) ? "TD" : ((c.cgender == 1) ? "NM" : "XD");
					string clan = (c.clan != null && !string.IsNullOrEmpty(c.clan.name)) ? c.clan.name : "";

					MapTargetInfo info = new MapTargetInfo
					{
						isBoss = false,
						playerName = c.cName,
						planet = planet,
						clan = clan,
						hp = c.cHP,
						hpFull = (c.cHPFull > 0) ? c.cHPFull : c.cHP,
						cx = c.cx,
						cy = c.cy,
						charRef = c
					};
					players.Add(info);
				}
			}
		}

		currentMapTargets.Clear();
		currentMapTargets.AddRange(bosses);
		currentMapTargets.AddRange(players);

		if (currentMapTargets.Count > 10)
		{
			currentMapTargets = currentMapTargets.GetRange(0, 10);
		}
	}

	/// <summary>
	/// Xử lý tương tác khi click vào mục tiêu trong danh sách người / boss trong khu:
	/// - Lần 1: Chỉ định focus vào mục tiêu (không khóa focus vĩnh viễn, góc trên trái hiển thị profile).
	/// - Lần 2: Dịch chuyển tới đứng cạnh mục tiêu đó luôn.
	/// </summary>
	public static bool HandleLeftClick(int mousePx, int mousePy)
	{
		// Chặn click nếu đang mở dialog/menu/panel/chat hoặc vừa đóng dialog (< 400ms)
		if (ModManager.isAnyDialogOpen || (GameCanvas.panel != null && GameCanvas.panel.isShow) || (GameCanvas.menu != null && GameCanvas.menu.showMenu) || (ChatTextField.gI() != null && ChatTextField.gI().isShow) || (mSystem.currentTimeMillis() - ModManager.lastDialogClickTime < 400L))
		{
			return false;
		}

		if (currentMapTargets == null || currentMapTargets.Count == 0)
		{
			return false;
		}

		for (int i = 0; i < currentMapTargets.Count; i++)
		{
			MapTargetInfo target = currentMapTargets[i];
			if (target != null && mousePx >= target.hitX && mousePx <= target.hitX + target.hitW &&
				mousePy >= target.hitY && mousePy <= target.hitY + target.hitH)
			{
				long now = mSystem.currentTimeMillis();

				bool isSameTarget = false;
				if (lastClickedTarget != null)
				{
					if (target.charRef != null && lastClickedTarget.charRef == target.charRef)
					{
						isSameTarget = true;
					}
					else if (target.mobRef != null && lastClickedTarget.mobRef == target.mobRef)
					{
						isSameTarget = true;
					}
					else if (!string.IsNullOrEmpty(target.playerName) && target.playerName == lastClickedTarget.playerName)
					{
						isSameTarget = true;
					}
					else if (!string.IsNullOrEmpty(target.bossName) && target.bossName == lastClickedTarget.bossName)
					{
						isSameTarget = true;
					}
				}

				if (isSameTarget && (now - lastClickTargetTime < 3500L))
				{
					// CLICK LẦN THỨ 2: Dịch chuyển tới đứng cạnh mục tiêu
					TeleportToTarget(target);
					lastClickedTarget = null;
				}
				else
				{
					// CLICK LẦN 1: Focus vào mục tiêu (không khóa focus, người chơi vẫn tự do đổi target)
					FocusTarget(target);
					lastClickedTarget = target;
					lastClickTargetTime = now;
				}

				GameCanvas.isPointerJustRelease = false;
				return true;
			}
		}

		return false;
	}

	public static bool HandleRightClick(int mousePx, int mousePy)
	{
		return HandleLeftClick(mousePx, mousePy);
	}

	/// <summary>
	/// Trỏ focus tới mục tiêu cho lần click đầu tiên (hoàn toàn không khóa focus).
	/// </summary>
	public static void FocusTarget(MapTargetInfo target)
	{
		if (target == null) return;
		Char my = Char.myCharz();
		if (my == null) return;

		if (target.charRef != null)
		{
			my.charFocus = target.charRef;
			my.mobFocus = null;
			my.npcFocus = null;
			my.itemFocus = null;
			SoundMn.gI().buttonClick();
			GameScr.info1.addInfo("Focus: " + target.playerName, 0);
		}
		else if (target.mobRef != null)
		{
			my.mobFocus = target.mobRef;
			my.charFocus = null;
			my.npcFocus = null;
			my.itemFocus = null;
			SoundMn.gI().buttonClick();
			GameScr.info1.addInfo("Focus: " + target.bossName, 0);
		}
	}

	/// <summary>
	/// Dịch chuyển tức thời tới đứng cạnh mục tiêu trong map.
	/// </summary>
	public static void TeleportToTarget(MapTargetInfo target)
	{
		Char my = Char.myCharz();
		if (my == null || target == null) return;

		int tx = target.cx;
		int ty = target.cy;

		// Nếu đối tượng đang sống, lấy tọa độ mới nhất thực tế
		if (target.charRef != null)
		{
			tx = target.charRef.cx;
			ty = target.charRef.cy;
		}
		else if (target.mobRef != null)
		{
			tx = target.mobRef.x;
			ty = target.mobRef.y;
		}

		// Đứng cạnh user đó: cách khoảng 24px sang trái hoặc phải
		int offsetX = (my.cx >= tx) ? 24 : -24;
		int standX = tx + offsetX;
		int standY = ty;

		my.cx = standX;
		my.cy = standY;
		my.cxSend = standX;
		my.cySend = standY;

		bool hasGround = TileMap.tileTypeAt(standX, standY + 12, 2);
		if (hasGround)
		{
			my.statusMe = 1; // Đứng trên mặt đất
		}
		else
		{
			my.statusMe = 10; // Bay trên không
		}
		my.cvx = 0;
		my.cvy = 0;
		my.delayFall = 0;

		// Đảm bảo vẫn focus vào đối tượng
		if (target.charRef != null)
		{
			my.charFocus = target.charRef;
		}
		else if (target.mobRef != null)
		{
			my.mobFocus = target.mobRef;
		}

		Service.isForceSendMove = true;
		Service.gI().charMove();

		string name = target.isBoss ? target.bossName : target.playerName;
		GameScr.info1.addInfo("Dịch tới cạnh: " + name, 0);
		SoundMn.gI().buttonClick();
	}
}
