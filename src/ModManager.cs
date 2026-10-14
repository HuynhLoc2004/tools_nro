using System;
using System.Collections.Generic;
using UnityEngine;

public class ModManager : IActionListener
{
	private static ModManager instance;

	// Trạng thái các chức năng (Mặc định TẤT CẢ TẮT - Chỉ khi người dùng tích mới bật)
	public static bool isTanSat = false;
	public static bool isAutoTrain = false;
	public static bool isAutoChangeZone = false;
	public static bool isAutoItem = false;
	public static bool isAutoPick = false;      // Tự động nhặt vật phẩm (Mặc định TẮT)
	public static bool isBuaThuHut = false;     // Bùa thu hút (Mặc định TẮT)
	public static bool isShowHUD = false;       // Bảng HUD màn hình (Mặc định TẮT để không che màn hình)

	// Trạng thái hiển thị các Bảng Cài Đặt (Popup Panels)
	public static bool isShowMainDialog = false;  // Bảng Điều Khiển Tổng Hợp
	public static bool isShowSkillDialog = false; // Bảng Select Skill (Hình 3)
	public static bool isShowItemDialog = false;  // Bảng Auto Item 2 cột (Hình 1 & 2)
	public static bool isShowPickDialog = false;  // Bảng Auto Nhặt (Hình mẫu người dùng)

	public static bool isAnyDialogOpen
	{
		get
		{
			if (GameCanvas.currentDialog != null)
			{
				return false;
			}
			return isShowMainDialog || isShowSkillDialog || isShowItemDialog || isShowPickDialog;
		}
	}

	public static bool isWaitingDropConfirm = false;
	public static int activeInputIndex = -1; // -1: không focus, 0: Nhặt, 1: Loại trừ, 2: Vứt bỏ

	public static void closeAllDialogs()
	{
		isShowMainDialog = false;
		isShowSkillDialog = false;
		isShowItemDialog = false;
		isShowPickDialog = false;
		activeInputIndex = -1;
	}

	public static bool isItemInDropList(int templateId)
	{
		List<int> dropIds = parseFilterIds(dropItemFilter);
		return dropIds.Contains(templateId);
	}

	private static void handleInputBackspace()
	{
		if (activeInputIndex == 0)
		{
			if (!string.IsNullOrEmpty(pickItemFilter))
				pickItemFilter = pickItemFilter.Substring(0, pickItemFilter.Length - 1);
		}
		else if (activeInputIndex == 1)
		{
			if (!string.IsNullOrEmpty(excludeItemFilter))
				excludeItemFilter = excludeItemFilter.Substring(0, excludeItemFilter.Length - 1);
		}
		else if (activeInputIndex == 2)
		{
			if (!string.IsNullOrEmpty(dropItemFilter))
				dropItemFilter = dropItemFilter.Substring(0, dropItemFilter.Length - 1);
		}
		else if (activeInputIndex == 3)
		{
			if (!string.IsNullOrEmpty(eatItemFilter))
				eatItemFilter = eatItemFilter.Substring(0, eatItemFilter.Length - 1);
		}
	}

	private static void handleInputChar(char c)
	{
		if (activeInputIndex == 0)
		{
			if (pickItemFilter == null) pickItemFilter = string.Empty;
			if (pickItemFilter.Length < 60) pickItemFilter += c;
		}
		else if (activeInputIndex == 1)
		{
			if (excludeItemFilter == null) excludeItemFilter = string.Empty;
			if (excludeItemFilter.Length < 60) excludeItemFilter += c;
		}
		else if (activeInputIndex == 2)
		{
			if (dropItemFilter == null) dropItemFilter = string.Empty;
			if (dropItemFilter.Length < 60) dropItemFilter += c;
		}
		else if (activeInputIndex == 3)
		{
			if (eatItemFilter == null) eatItemFilter = string.Empty;
			if (eatItemFilter.Length < 60) eatItemFilter += c;
		}
	}

	// Cài đặt chức năng Auto Nhặt & Tự vứt rác
	public static bool isPickOthers = false;      // Nhặt vật phẩm của người khác
	public static bool isPickAllMap = true;       // Nhặt toàn map (Teleport / Bùa)
	public static bool isAutoDrop = true;         // Tự động vứt vật phẩm

	public static string pickItemFilter = "";
	public static string excludeItemFilter = "";
	public static string dropItemFilter = "74, 18, 1212, 888, 211, 212, 221";
	public static string eatItemFilter = "";

	private static long lastDropItemTime = 0L;
	private static long lastDialogClickTime = 0L;
	private static Dictionary<int, long> lastTimeUsedCustomItems = new Dictionary<int, long>();

	public static void onZoneChangeBlocked()
	{
		isWaitingZoneData = false;
		lastZoneChangeTime = mSystem.currentTimeMillis();
	}

	public static bool isPlayerManuallyMoving()
	{
		try
		{
			if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) ||
			    Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
			{
				return true;
			}
			if (GameCanvas.keyHold[(!Main.isPC) ? 2 : 21] || GameCanvas.keyHold[(!Main.isPC) ? 8 : 22] ||
			    GameCanvas.keyHold[(!Main.isPC) ? 4 : 23] || GameCanvas.keyHold[(!Main.isPC) ? 6 : 24])
			{
				return true;
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	public static bool isPlayerBusyInUI()
	{
		try
		{
			// Không chặn khi mở Panel Hành trang - cho phép vừa mở hành trang vừa đánh quái mượt mà
			if (GameCanvas.menu != null && GameCanvas.menu.showMenu) return true;
			if (ChatTextField.isShowing) return true;
			if (GameCanvas.currentDialog != null) return true;
		}
		catch (Exception)
		{
		}
		return false;
	}

	// =============================================================
	// CÀI ĐẶT AUTO ITEM (2 CỘT CHUẨN THEO HÌNH 1 & 2)
	// =============================================================
	public static string[] itemNamesCol1 = new string[]
	{
		"Cuồng nộ",
		"Giáp Xên bọ hung",
		"Bổ huyết",
		"Bổ khí",
		"Sát thương chuẩn",
		"Bánh Pudding",
		"Kem dâu",
		"Sushi",
		"Bạch tuộc nướng",
		"Ẩn danh",
		"Nho tím",
		"Tự động luyện tập"
	};

	public static string[] itemNamesCol2 = new string[]
	{
		"Cuồng nộ 2",
		"Giáp Xên bọ hung 2",
		"Bổ huyết 2",
		"Bổ khí 2",
		"Sát thương chuẩn 2",
		"Xúc xích",
		"Mì ly",
		"Cua rang me",
		"Tôm tẩm bột chiên xù",
		"Ẩn danh 2",
		"Nho xanh",
		"Bùa x2 tn,sm đệ tử"
	};

	public static bool[] itemCol1Selected = new bool[12];
	public static bool[] itemCol2Selected = new bool[12];

	public static long[] lastTimeUsedCol1 = new long[12];
	public static long[] lastTimeUsedCol2 = new long[12];

	public static int scrollItemIndex = 0;
	private static long lastTimeCheckItem = 0L;

	// =============================================================
	// CÀI ĐẶT TỐC ĐỘ GAME (CHEAT SPEED)
	// =============================================================
	public static float currentSpeed = 3f;

	// =============================================================
	// CÀI ĐẶT SKILL DÙNG KHI TÀN SÁT & TỰ LUYỆN TẬP
	// =============================================================
	public static List<int> selectedSkillIds = new List<int>();
	public static int scrollSkillIndex = 0;

	// Quản lý chuyển khu & tấn công
	public static bool isWaitingZoneData = false;
	private static long lastZoneRequestTime = 0L;
	private static long lastZoneChangeTime = 0L;
	private static int currentZoneID = -1;
	private static long lastAttackTime = 0L;
	private static long lastPickItemTime = 0L;
	private static long lastTimeEatPean = 0L;
	private static long lastMoveSendTime = 0L;

	public static int lastUsedItemTemplateId = -1;
	public static long lastUsedItemSendTime = 0L;
	public static Dictionary<int, short> itemBuffIconMap = new Dictionary<int, short>();
	public static short lastFoodBuffIconId = -1;
	public static long lastTimeEatFood = 0L;

	// Quản lý dịch map nhanh (J: Trái, K: Giữa, L: Phải)
	public static long lastChangeMapRequestTime = 0L;
	public static int lastChangeMapFromMapId = -1;
	public static long lastChangeMapSuccessTime = 0L;
	public static Waypoint currentTeleportWaypoint = null;
	public static Waypoint pendingWaypoint = null;
	public static long pendingChangeMapSendTime = 0L;
	public static int teleportTargetX = -1;
	public static int teleportTargetY = -1;
	public static long lastTeleportRequestMapTime = 0L;
	public static int teleportRequestCount = 0;

	public static bool isPathTeleporting = false;
	public static int pathCurrentX = 0;
	public static int pathCurrentY = 0;
	public static int pathTargetX = 0;
	public static int pathTargetY = 0;
	public static Waypoint pathPendingWaypoint = null;
	public static long lastPathStepTime = 0L;
	private static long lastTimeCheckTDLT = 0L;

	public static void sendDirectMove(int x, int y)
	{
		try
		{
			Char myChar = Char.myCharz();
			if (myChar != null)
			{
				myChar.cxSend = x;
				myChar.cySend = y;
				myChar.cdirSend = myChar.cdir;
			}
			Message message = new Message((sbyte)(-7));
			sbyte inAir = (sbyte)(TileMap.tileTypeAt(x / TileMap.size, y / TileMap.size) == 0 ? 1 : 0);
			message.writer().writeByte(inAir);
			message.writer().writeShort(x);
			message.writer().writeShort(y);
			Session_ME.gI().sendMessage(message);
			message.cleanup();
		}
		catch (Exception)
		{
		}
	}

	public static void updatePathTeleport(long now)
	{
		try
		{
			if (now - lastPathStepTime < 25L)
			{
				return;
			}
			lastPathStepTime = now;

			Char myChar = Char.myCharz();
			if (myChar == null || Char.isLoadingMap)
			{
				isPathTeleporting = false;
				pathPendingWaypoint = null;
				return;
			}

			int dist = Res.distance(pathCurrentX, pathCurrentY, pathTargetX, pathTargetY);
			int stepSize = 95;

			if (dist <= stepSize)
			{
				pathCurrentX = pathTargetX;
				pathCurrentY = pathTargetY;
				myChar.cx = pathTargetX;
				myChar.cy = pathTargetY;
				myChar.cvy = 0;
				myChar.cvx = 0;
				bool hasGround = TileMap.tileTypeAt(pathTargetX, pathTargetY + 12, 2);
				myChar.statusMe = hasGround ? 1 : 10;
				sendDirectMove(pathTargetX, pathTargetY);
				isPathTeleporting = false;

				if (pathPendingWaypoint != null)
				{
					Waypoint wp = pathPendingWaypoint;
					pathPendingWaypoint = null;
					doRequestChangeMap(wp, pathTargetX, pathTargetY);
				}
			}
			else
			{
				pathCurrentX += (pathTargetX - pathCurrentX) * stepSize / dist;
				pathCurrentY += (pathTargetY - pathCurrentY) * stepSize / dist;
				myChar.cx = pathCurrentX;
				myChar.cy = pathCurrentY;
				myChar.cdir = (pathTargetX > pathCurrentX) ? 1 : -1;
				myChar.statusMe = 2; // Hiển thị lướt / chạy cực mượt
				myChar.cvy = 0;
				myChar.cvx = 0;
				sendDirectMove(pathCurrentX, pathCurrentY);
			}
		}
		catch (Exception)
		{
			isPathTeleporting = false;
			pathPendingWaypoint = null;
		}
	}

	public static bool isMapChangeCooldown()
	{
		long cd = (long)Mathf.Max(100f, 350f / Mathf.Sqrt(currentSpeed));
		return mSystem.currentTimeMillis() - lastChangeMapSuccessTime < cd;
	}

	public static void onMapLoaded()
	{
		lastChangeMapSuccessTime = mSystem.currentTimeMillis();
		lastChangeMapRequestTime = 0L;
		lastChangeMapFromMapId = TileMap.mapID;
		currentTeleportWaypoint = null;
		teleportRequestCount = 0;
		Char.ischangingMap = false;
		Char.isLockKey = false;
		isPathTeleporting = false;
		pathPendingWaypoint = null;
		InfoDlg.hide();
		GameCanvas.endDlg();
		GameCanvas.currentDialog = null;

		// Nudge nhân vật ra khỏi cổng vừa bước ra nếu đang đứng đè lên cổng để tránh bị hút ngược lại map cũ
		try
		{
			Char myChar = Char.myCharz();
			if (myChar != null && TileMap.vGo != null)
			{
				for (int i = 0; i < TileMap.vGo.size(); i++)
				{
					Waypoint wp = (Waypoint)TileMap.vGo.elementAt(i);
					if (wp != null && !wp.isEnter)
					{
						if (myChar.cx >= wp.minX && myChar.cx <= wp.maxX && myChar.cy >= wp.minY && myChar.cy <= wp.maxY)
						{
							if (myChar.cx < TileMap.pxw / 2)
							{
								myChar.cx = wp.maxX + 36;
							}
							else
							{
								myChar.cx = wp.minX - 36;
							}
							if (myChar.cx < 24) myChar.cx = 24;
							if (myChar.cx > TileMap.pxw - 24) myChar.cx = TileMap.pxw - 24;
							sendDirectMove(myChar.cx, myChar.cy);
							break;
						}
					}
				}
			}
		}
		catch (Exception)
		{
		}
	}

	static ModManager()
	{
		// Mặc định TẤT CẢ TẮT - Không tự động tích hay tự động dùng bất cứ thứ gì khi chưa được người dùng chọn
		loadSettings();
	}

	public static ModManager gI()
	{
		if (instance == null)
		{
			instance = new ModManager();
		}
		return instance;
	}

	// -------------------------------------------------------------
	// XỬ LÝ LỆNH CHAT
	// -------------------------------------------------------------
	public static bool onChat(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}

		string cmd = text.Trim().ToLower();

		if (cmd.Equals("ts"))
		{
			isTanSat = !isTanSat;
			notify("Tàn sát: " + (isTanSat ? "BẬT" : "TẮT"));
			return true;
		}

		if (cmd.Equals("ak"))
		{
			isAutoTrain = !isAutoTrain;
			notify("Tự luyện tập: " + (isAutoTrain ? "BẬT" : "TẮT"));
			return true;
		}

		if (cmd.Equals("ck"))
		{
			isAutoChangeZone = !isAutoChangeZone;
			notify("Auto đổi khu khi hết quái: " + (isAutoChangeZone ? "BẬT" : "TẮT"));
			return true;
		}

		if (cmd.Equals("item"))
		{
			isAutoItem = !isAutoItem;
			notify("Auto Item (Đồ ăn, Cuồng nộ): " + (isAutoItem ? "BẬT" : "TẮT"));
			return true;
		}

		if (cmd.StartsWith("vut ") || cmd.StartsWith("drop "))
		{
			string newFilter = text.Substring(text.IndexOf(' ') + 1).Trim();
			dropItemFilter = newFilter;
			isAutoDrop = true;
			notify("Đã lưu D/S Vứt Bỏ: " + dropItemFilter + " (Auto Vứt: BẬT)");
			return true;
		}

		if (cmd.Equals("vut") || cmd.Equals("drop") || cmd.Equals("cvut"))
		{
			isAutoDrop = !isAutoDrop;
			notify("Tự Động Vứt Vật Phẩm: " + (isAutoDrop ? "BẬT" : "TẮT"));
			return true;
		}

		if (cmd.StartsWith("nhat ") || cmd.StartsWith("pick "))
		{
			string newFilter = text.Substring(text.IndexOf(' ') + 1).Trim();
			pickItemFilter = newFilter;
			isAutoPick = true;
			notify("Đã lưu D/S Item Nhặt: " + pickItemFilter + " (Auto Nhặt: BẬT)");
			return true;
		}

		if (cmd.StartsWith("loai ") || cmd.StartsWith("exclude "))
		{
			string newFilter = text.Substring(text.IndexOf(' ') + 1).Trim();
			excludeItemFilter = newFilter;
			notify("Đã lưu D/S Loại Trừ: " + excludeItemFilter);
			return true;
		}

		if (cmd.Equals("nhat") || cmd.Equals("anhat") || cmd.Equals("cnhat"))
		{
			showAutoPickMenu();
			return true;
		}

		if (cmd.Equals("bua") || cmd.Equals("thuhut") || cmd.Equals("hutdo"))
		{
			isBuaThuHut = !isBuaThuHut;
			notify("Bùa thu hút (Hút đồ): " + (isBuaThuHut ? "BẬT" : "TẮT"));
			return true;
		}

		if (cmd.Equals("hud"))
		{
			isShowHUD = !isShowHUD;
			notify("Bảng trạng thái HUD: " + (isShowHUD ? "BẬT" : "TẮT"));
			return true;
		}

		if (cmd.Equals("cs") || cmd.Equals("skill") || cmd.Equals("cskill"))
		{
			showSelectSkillMenu();
			return true;
		}

		if (cmd.Equals("citem"))
		{
			showAutoItemMenu();
			return true;
		}

		if (cmd.Equals("menu") || cmd.Equals("mod"))
		{
			showMainControlDialog();
			return true;
		}

		// Xem ID vật phẩm (dưới đất hoặc trong hành trang)
		if (cmd.Equals("id") || cmd.Equals("xemid") || cmd.Equals("iditem"))
		{
			Char myChar = Char.myCharz();
			if (myChar != null)
			{
				if (myChar.itemFocus != null && myChar.itemFocus.template != null)
				{
					notify("Item đất: " + myChar.itemFocus.template.name + " [ID: " + myChar.itemFocus.template.id + "]");
					return true;
				}
				if (myChar.arrItemBag != null)
				{
					string bagInfo = "ID túi: ";
					int c = 0;
					for (int i = 0; i < myChar.arrItemBag.Length; i++)
					{
						Item it = myChar.arrItemBag[i];
						if (it != null && it.template != null && c < 8)
						{
							bagInfo += it.template.name + "[" + it.template.id + "], ";
							c++;
						}
					}
					notify(bagInfo.TrimEnd(' ', ','));
					return true;
				}
			}
			return true;
		}

		// Lệnh cài đặt ID Tự Ăn / Dùng
		if (cmd.StartsWith("an "))
		{
			eatItemFilter = text.Substring(3).Trim();
			isAutoItem = true;
			notify("D/S ItemID Tự Ăn: " + eatItemFilter);
			return true;
		}
		if (cmd.StartsWith("aan "))
		{
			string val = text.Substring(4).Trim();
			eatItemFilter = string.IsNullOrEmpty(eatItemFilter) ? val : (eatItemFilter + "," + val);
			isAutoItem = true;
			notify("Đã thêm ID Tự Ăn: " + eatItemFilter);
			return true;
		}
		if (cmd.Equals("xoaan"))
		{
			eatItemFilter = "";
			notify("Đã xóa D/S ID Tự Ăn");
			return true;
		}

		// Lệnh cài đặt ID Nhặt
		if (cmd.StartsWith("nhat "))
		{
			pickItemFilter = text.Substring(5).Trim();
			isAutoPick = true;
			notify("D/S ItemID Nhặt: " + pickItemFilter);
			return true;
		}
		if (cmd.StartsWith("anhat "))
		{
			string val = text.Substring(6).Trim();
			pickItemFilter = string.IsNullOrEmpty(pickItemFilter) ? val : (pickItemFilter + "," + val);
			isAutoPick = true;
			notify("Đã thêm ID Nhặt: " + pickItemFilter);
			return true;
		}
		if (cmd.Equals("xoanhat"))
		{
			pickItemFilter = "";
			notify("Đã xóa D/S ID Nhặt");
			return true;
		}

		// Lệnh cài đặt ID Vứt Bỏ
		if (cmd.StartsWith("vut "))
		{
			dropItemFilter = text.Substring(4).Trim();
			isAutoDrop = true;
			notify("D/S ItemID Vứt Bỏ: " + dropItemFilter);
			return true;
		}
		if (cmd.StartsWith("avut "))
		{
			string val = text.Substring(5).Trim();
			dropItemFilter = string.IsNullOrEmpty(dropItemFilter) ? val : (dropItemFilter + "," + val);
			isAutoDrop = true;
			notify("Đã thêm ID Vứt Bỏ: " + dropItemFilter);
			return true;
		}
		if (cmd.Equals("xoavut"))
		{
			dropItemFilter = "";
			notify("Đã xóa D/S ID Vứt Bỏ");
			return true;
		}

		// Lệnh cài đặt ID Loại Trừ
		if (cmd.StartsWith("loai "))
		{
			excludeItemFilter = text.Substring(5).Trim();
			notify("D/S ItemID Loại Trừ: " + excludeItemFilter);
			return true;
		}
		if (cmd.StartsWith("aloai "))
		{
			string val = text.Substring(6).Trim();
			excludeItemFilter = string.IsNullOrEmpty(excludeItemFilter) ? val : (excludeItemFilter + "," + val);
			notify("Đã thêm ID Loại Trừ: " + excludeItemFilter);
			return true;
		}
		if (cmd.Equals("xoaloai"))
		{
			excludeItemFilter = "";
			notify("Đã xóa D/S ID Loại Trừ");
			return true;
		}

		// Cheat speed: s1 -> s10 (hoặc bất kỳ giá trị nào từ 1 đến 10)
		if (cmd.StartsWith("s") && cmd.Length > 1 && (char.IsDigit(cmd[1]) || cmd[1] == '.'))
		{
			string numStr = cmd.Substring(1).Trim();
			if (float.TryParse(numStr, out float val))
			{
				val = Mathf.Clamp(val, 1f, 10f);
				setSpeed(val);
				return true;
			}
		}

		return false;
	}

	public static void setSpeed(float speed)
	{
		currentSpeed = Mathf.Clamp(speed, 1f, 10f);
		Time.timeScale = currentSpeed;
		try
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 120;
		}
		catch (Exception)
		{
		}
		if (Char.myCharz() != null)
		{
			Char.myCharz().updateSpeed();
		}
		notify("Tốc độ game & di chuyển: x" + currentSpeed);
	}

	public static void notify(string message)
	{
		if (GameScr.info1 != null)
		{
			GameScr.info1.addInfo(message, 0);
		}
	}

	// -------------------------------------------------------------
	// VÒNG LẶP UPDATE (GameScr.update)
	// -------------------------------------------------------------
	public static void update()
	{
		try
		{
			if (Char.myCharz() == null || TileMap.mapID == -1)
			{
				return;
			}

			if (GameCanvas.gameTick % 30 == 0)
			{
				Char.myCharz().updateSpeed();
			}

			long now = mSystem.currentTimeMillis();

			// Tắt hẳn autoPlay mặc định của game để không bị xung đột, giật cục, loạn mục tiêu
			GameScr.isAutoPlay = false;

			// (Input của các dialog được GameScr.updateKey() xử lý độc quyền, không gọi trùng lặp tại đây để tránh click đúp)

			// 1. Tự động dùng Trị thương (Cứu) hoặc Tái tạo năng lượng khi HP/KI xuống thấp
			checkAutoBuffOrHeal(now);

			// 2. Tàn Sát quái trong map (nếu bật)
			if (isTanSat)
			{
				updateTanSat(now);
			}
			// 3. Tự động luyện tập (nếu bật độc lập khi không tàn sát)
			else if (isAutoTrain)
			{
				updateAutoTrain(now);
			}

			// 4. Tự động đổi khu khi hết quái (chạy độc lập, áp dụng cho cả Tàn Sát & Tự Luyện Tập)
			if (isAutoChangeZone)
			{
				updateAutoChangeZone(now);
			}

			// 5. Auto Item (Đồ ăn, Cuồng nộ, Tự động luyện tập)
			if (isAutoItem)
			{
				updateAutoItem(now);
			}

			// 6. Tự động nhặt vật phẩm & Bùa thu hút (Hút đồ vào túi)
			if (isAutoPick || isBuaThuHut || isPickAllMap)
			{
				updateAutoPick(now);
			}

			// 7. Tự động vứt vật phẩm theo D/S ItemID
			if (isAutoDrop)
			{
				updateAutoDrop(now);
			}

			// Cập nhật dịch chuyển vi mô an toàn cho người không có TDLT
			if (isPathTeleporting)
			{
				updatePathTeleport(now);
			}

			// Tự động đóng dialog "Xin chờ" nếu map đã đổi hoặc không còn trạng thái chuyển map
			if (!Char.ischangingMap && !Char.isLoadingMap && InfoDlg.isShow)
			{
				if (TileMap.mapID != lastChangeMapFromMapId && lastChangeMapFromMapId != -1)
				{
					InfoDlg.hide();
					GameCanvas.endDlg();
					GameCanvas.currentDialog = null;
				}
			}

			// 8. Quản lý trạng thái chuyển map (chống kẹt / an toàn)
			if (Char.ischangingMap)
			{
				if (TileMap.mapID != lastChangeMapFromMapId)
				{
					currentTeleportWaypoint = null;
					onMapLoaded();
				}
				else
				{
					// Failsafe giải phóng khóa sau 1500ms nếu map không đổi (tránh treo 'Xin chờ')
					if (now - lastChangeMapRequestTime > 1500L)
					{
						Char.ischangingMap = false;
						Char.isLockKey = false;
						InfoDlg.hide();
						GameCanvas.endDlg();
						GameCanvas.currentDialog = null;
						currentTeleportWaypoint = null;
						pendingWaypoint = null;
						isPathTeleporting = false;
						lastChangeMapRequestTime = 0L;
					}
				}
			}
		}
		catch (Exception)
		{
		}
	}

	// =============================================================
	// TÍNH NĂNG DỊCH MAP TỨC THÌ BẰNG PHÍM J, K, L (CHUẨN KHÔNG LAG/DISCONNECT)
	// =============================================================
	public static void checkHotkeysEveryFrame()
	{
		try
		{
			if (isPlayerBusyInUI()) return;
			// Phím F2: Bật / Tắt Bảng Cài Đặt Mod
			if (Input.GetKeyDown(KeyCode.F2))
			{
				if (isShowMainDialog || isShowSkillDialog || isShowItemDialog || isShowPickDialog)
				{
					closeAllDialogs();
				}
				else
				{
					showMainControlDialog();
				}
				return;
			}

			// Phím J: Dịch map TRÁI (hoặc cuối map trái)
			if (Input.GetKeyDown(KeyCode.J))
			{
				handleQuickMapChange(0);
			}
			// Phím K: Dịch map GIỮA (hoặc giữa map)
			else if (Input.GetKeyDown(KeyCode.K))
			{
				handleQuickMapChange(1);
			}
			// Phím L: Dịch map PHẢI (hoặc cuối map phải)
			else if (Input.GetKeyDown(KeyCode.L))
			{
				handleQuickMapChange(2);
			}
		}
		catch (Exception)
		{
		}
	}

	private static void checkHotkeyChangeMap()
	{
		checkHotkeysEveryFrame();
	}

	public static int findGroundY(int x)
	{
		Char myChar = Char.myCharz();
		int startY = (myChar != null) ? (myChar.cy - 30) : 24;
		if (startY < 24) startY = 24;

		for (int y = startY; y < TileMap.pxh - 24; y += 12)
		{
			if (TileMap.tileTypeAt(x, y, 2))
			{
				return (y / 24) * 24;
			}
		}
		for (int y = 24; y < TileMap.pxh - 24; y += 12)
		{
			if (TileMap.tileTypeAt(x, y, 2))
			{
				return (y / 24) * 24;
			}
		}
		return (myChar != null) ? myChar.cy : 300;
	}

	public static bool isRealTDLTActive()
	{
		try
		{
			Char myChar = Char.myCharz();
			if (myChar == null) return false;

			// 1. Kiểm tra Giáp Luyện Tập đang mặc trên người (arrItemBody)
			if (myChar.arrItemBody != null)
			{
				for (int i = 0; i < myChar.arrItemBody.Length; i++)
				{
					Item it = myChar.arrItemBody[i];
					if (it != null && it.template != null)
					{
						int id = it.template.id;
						if (id == 529 || id == 530 || id == 531 || id == 521)
						{
							return true;
						}
						if (!string.IsNullOrEmpty(it.template.name))
						{
							string name = it.template.name.ToLower();
							if (name.Contains("luyện tập") || name.Contains("luyen tap") || name.Contains("tập luyện") || name.Contains("tap luyen"))
							{
								return true;
							}
						}
					}
				}
			}

			// 2. Quét Char.vItemTime (Icon bánh răng vàng đặc trưng của TDLT)
			if (Char.vItemTime != null)
			{
				for (int j = 0; j < Char.vItemTime.size(); j++)
				{
					ItemTime t = (ItemTime)Char.vItemTime.elementAt(j);
					if (t != null)
					{
						if (t.idIcon == 4387 || t.idIcon == 4028 || t.idIcon == 521 || t.idIcon == 4330)
						{
							return true;
						}
					}
				}
			}

			// 3. Quét GameScr.textTime nếu có
			if (GameScr.textTime != null)
			{
				for (int j = 0; j < GameScr.textTime.size(); j++)
				{
					ItemTime t2 = (ItemTime)GameScr.textTime.elementAt(j);
					if (t2 != null && !string.IsNullOrEmpty(t2.text))
					{
						string s = t2.text.ToLower();
						if (s.Contains("luyện tập") || s.Contains("luyen tap") || s.Contains("tự động") || s.Contains("tu dong"))
						{
							return true;
						}
					}
				}
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	public static bool hasTrainingGearOrBuff()
	{
		return isRealTDLTActive();
	}

	public static int findAutoTrainBagIndex()
	{
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.arrItemBag == null) return -1;
		for (int i = 0; i < myChar.arrItemBag.Length; i++)
		{
			Item it = myChar.arrItemBag[i];
			if (it != null && it.template != null)
			{
				int id = it.template.id;
				if (id == 529 || id == 530 || id == 531 || id == 521 ||
					it.template.iconID == 4387 || it.template.iconID == 4028 || it.template.iconID == 521 || it.template.iconID == 4330)
				{
					return i;
				}
				if (!string.IsNullOrEmpty(it.template.name))
				{
					string name = it.template.name.ToLower();
					if (name.Contains("luyện tập") || name.Contains("luyen tap") || name.Contains("máy luyện") || name.Contains("tập luyện") || name.Contains("tự động") || name.Contains("tu dong"))
					{
						return i;
					}
				}
			}
		}
		return -1;
	}

	public static void checkAutoActivateTDLT()
	{
		try
		{
			if (!hasTrainingGearOrBuff())
			{
				int bagIdx = findAutoTrainBagIndex();
				if (bagIdx >= 0)
				{
					Service.gI().useItem(0, 1, (sbyte)bagIdx, -1);
					notify("Tự mặc Giáp / Bật Luyện Tập!");
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static void teleportToGround(int dir)
	{
		Char myChar = Char.myCharz();
		if (myChar == null) return;

		int targetX = (dir == 0) ? 24 : ((dir == 2) ? (TileMap.pxw - 24) : (TileMap.pxw / 2));

		// Nếu là mép trái hoặc phải, tìm vị trí có đất gần nhất trên đảo (tránh rơi xuống biển ở map đảo như Đảo Kame)
		if (dir == 0)
		{
			for (int x = 24; x < TileMap.pxw / 2; x += 24)
			{
				int y = findGroundY(x);
				if (TileMap.tileTypeAt(x, y, 2))
				{
					targetX = x;
					break;
				}
			}
		}
		else if (dir == 2)
		{
			for (int x = TileMap.pxw - 24; x > TileMap.pxw / 2; x -= 24)
			{
				int y = findGroundY(x);
				if (TileMap.tileTypeAt(x, y, 2))
				{
					targetX = x;
					break;
				}
			}
		}

		int targetY = findGroundY(targetX);

		myChar.currentMovePoint = null;
		GameCanvas.clearKeyHold();
		GameCanvas.clearKeyPressed();

		GameScr.canAutoPlay = true;

		int startX = myChar.cx;
		int startY = myChar.cy;

		bool hasTDLT = isRealTDLTActive();

		if (hasTDLT)
		{
			myChar.cx = targetX;
			myChar.cy = targetY;
			myChar.statusMe = 1;
			myChar.cvy = 0;
			myChar.cvx = 0;
			myChar.delayFall = 50;
			isPathTeleporting = false;
			pathPendingWaypoint = null;
			sendDirectMove(targetX, targetY);
		}
		else
		{
			isPathTeleporting = true;
			pathCurrentX = startX;
			pathCurrentY = startY;
			pathTargetX = targetX;
			pathTargetY = targetY;
			pathPendingWaypoint = null;
			lastPathStepTime = 0L;
		}
	}

	public static void handleQuickMapChange(int dir)
	{
		long now = mSystem.currentTimeMillis();

		// 1. Không nhận phím khi map đang load dở
		if (Char.isLoadingMap || GameCanvas.isLoading)
		{
			return;
		}

		// 2. Cooldown ngắn mượt sau khi map vừa load xong
		long settleCooldown = 120L;
		if (now - lastChangeMapSuccessTime < settleCooldown)
		{
			return;
		}

		// 3. Cooldown phím (150ms)
		long requestCooldown = 150L;
		if (now - lastChangeMapRequestTime < requestCooldown)
		{
			return;
		}

		Char myChar = Char.myCharz();
		if (myChar == null) return;

		// Nếu đang lướt dở mà ấn phím khác, hủy lướt cũ ngay lập tức
		if (isPathTeleporting)
		{
			isPathTeleporting = false;
			pathPendingWaypoint = null;
		}

		if (Char.ischangingMap)
		{
			if (now - lastChangeMapRequestTime > 500L)
			{
				Char.ischangingMap = false;
				Char.isLockKey = false;
				InfoDlg.hide();
				GameCanvas.endDlg();
				GameCanvas.currentDialog = null;
			}
			else
			{
				return;
			}
		}

		lastChangeMapRequestTime = now;

		// Lấy danh sách tất cả Waypoint trong map hiện tại
		List<Waypoint> waypoints = new List<Waypoint>();
		if (TileMap.vGo != null)
		{
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint wp = (Waypoint)TileMap.vGo.elementAt(i);
				if (wp != null)
				{
					waypoints.Add(wp);
				}
			}
		}

		// Sắp xếp các waypoint theo tọa độ X từ trái sang phải
		waypoints.Sort((a, b) =>
		{
			int midA = (a.minX + a.maxX) / 2;
			int midB = (b.minX + b.maxX) / 2;
			return midA.CompareTo(midB);
		});

		int midMap = TileMap.pxw / 2;

		if (dir == 0) // J: Cổng Trái nhất (hoặc cuối map trái)
		{
			Waypoint leftWp = null;
			for (int i = 0; i < waypoints.Count; i++)
			{
				int wpX = (waypoints[i].minX + waypoints[i].maxX) / 2;
				if (wpX < midMap)
				{
					leftWp = waypoints[i];
					break; // Cổng đầu tiên bên trái của map
				}
			}

			if (leftWp != null)
			{
				teleportToWaypoint(leftWp, 0);
			}
			else
			{
				teleportToGround(0);
				notify("Cuối map (Trái)");
			}
		}
		else if (dir == 2) // L: Cổng Phải nhất (hoặc cuối map phải)
		{
			Waypoint rightWp = null;
			for (int i = waypoints.Count - 1; i >= 0; i--)
			{
				int wpX = (waypoints[i].minX + waypoints[i].maxX) / 2;
				if (wpX > midMap)
				{
					rightWp = waypoints[i];
					break; // Cổng đầu tiên bên phải của map
				}
			}

			if (rightWp != null)
			{
				teleportToWaypoint(rightWp, 2);
			}
			else
			{
				teleportToGround(2);
				notify("Cuối map (Phải)");
			}
		}
		else if (dir == 1) // K: Cổng Giữa (hoặc giữa map)
		{
			Waypoint midWp = null;
			int bestDist = int.MaxValue;
			for (int i = 0; i < waypoints.Count; i++)
			{
				int wpX = (waypoints[i].minX + waypoints[i].maxX) / 2;
				// Cổng giữa nằm ở khoảng trung tâm 20% - 80% map
				if (wpX >= TileMap.pxw * 20 / 100 && wpX <= TileMap.pxw * 80 / 100)
				{
					int dist = System.Math.Abs(wpX - midMap);
					if (dist < bestDist)
					{
						bestDist = dist;
						midWp = waypoints[i];
					}
				}
			}

			if (midWp != null)
			{
				teleportToWaypoint(midWp, 1);
			}
			else
			{
				teleportToGround(1);
				notify("Giữa map");
			}
		}
	}

	public static void teleportToWaypoint(Waypoint wp, int dir = -1)
	{
		if (wp == null) return;
		Char myChar = Char.myCharz();
		if (myChar == null) return;

		// 1. Xác định tọa độ X chuẩn (nằm hoàn toàn bên trong vùng cổng)
		int targetX = (wp.minX + wp.maxX) / 2;
		if (wp.minX <= 60)
		{
			targetX = wp.minX + 15;
			myChar.cdir = -1;
		}
		else if (wp.maxX >= TileMap.pxw - 60)
		{
			targetX = wp.maxX - 15;
			myChar.cdir = 1;
		}
		else
		{
			targetX = (wp.minX + wp.maxX) / 2;
			myChar.cdir = (targetX > TileMap.pxw / 2) ? 1 : -1;
		}

		// 2. Xác định tọa độ Y chuẩn
		int targetY = -1;
		for (int y = wp.maxY + 12; y >= wp.minY - 12; y -= 12)
		{
			if (TileMap.tileTypeAt(targetX, y, 2))
			{
				targetY = (y / 24) * 24;
				break;
			}
		}
		if (targetY == -1 || targetY < wp.minY || targetY > wp.maxY)
		{
			targetY = (wp.minY + wp.maxY) / 2;
		}

		myChar.currentMovePoint = null;
		GameCanvas.clearKeyHold();
		GameCanvas.clearKeyPressed();

		if (wp.popup != null)
		{
			wp.popup.isPaint = true;
		}
		if (PopUp.vPopups != null)
		{
			for (int i = 0; i < PopUp.vPopups.size(); i++)
			{
				PopUp p = (PopUp)PopUp.vPopups.elementAt(i);
				if (p != null) p.isPaint = true;
			}
		}

		string wpName = (wp.popup != null && wp.popup.says != null && wp.popup.says.Length > 0) ? wp.popup.says[0] : "Cổng";
		int dist = Res.distance(myChar.cx, myChar.cy, targetX, targetY);

		int startX = myChar.cx;
		int startY = myChar.cy;

		bool hasTDLT = isRealTDLTActive();

		if (hasTDLT)
		{
			// Có TDLT: Dịch chuyển tức thời 0ms
			myChar.cx = targetX;
			myChar.cy = targetY;
			bool hasGround = TileMap.tileTypeAt(targetX, targetY + 12, 2);
			myChar.statusMe = hasGround ? 1 : 10;
			myChar.cvy = 0;
			myChar.cvx = 0;
			myChar.delayFall = 50;
			isPathTeleporting = false;
			pathPendingWaypoint = null;
			sendDirectMove(targetX, targetY);
			doRequestChangeMap(wp, targetX, targetY);
		}
		else
		{
			// Không có TDLT: Lướt siêu tốc 65px/12ms mượt mà đến cổng, không spam 0ms, không ngắt kết nối
			isPathTeleporting = true;
			pathCurrentX = startX;
			pathCurrentY = startY;
			pathTargetX = targetX;
			pathTargetY = targetY;
			pathPendingWaypoint = wp;
			lastPathStepTime = 0L;
		}

		notify("Dịch map -> " + wpName);
	}

	private static void doRequestChangeMap(Waypoint wp, int targetX, int targetY)
	{
		if (wp == null) return;
		currentTeleportWaypoint = wp;
		teleportTargetX = targetX;
		teleportTargetY = targetY;
		lastChangeMapFromMapId = TileMap.mapID;
		lastChangeMapRequestTime = mSystem.currentTimeMillis();
		lastTeleportRequestMapTime = lastChangeMapRequestTime;
		teleportRequestCount = 0;

		Char.isLockKey = true;
		Char.ischangingMap = true;
		GameCanvas.clearKeyHold();
		GameCanvas.clearKeyPressed();
		InfoDlg.showWait();

		sendChangeMapPacket(wp);
	}

	public static void sendChangeMapPacket(Waypoint wp)
	{
		if (wp == null) return;

		if (wp.isOffline || TileMap.isTrainingMap())
		{
			Service.gI().getMapOffline();
		}
		else
		{
			Service.gI().requestChangeMap();
		}
	}

	// Kiểm tra skill có phải là skill hỗ trợ / buff / cứu người / nạp KI không (Không dùng để pem quái)
	public static bool isBuffOrSupportSkill(Skill s)
	{
		if (s == null || s.template == null)
		{
			return true;
		}
		int id = s.template.id;
		// 7: Trị thương (Cứu người / Hồi máu Namec)
		// 10: Tái tạo năng lượng (Hồi KI Namec)
		// 11: Thái Dương Hạ San
		// 14: Biến hình
		// 23: Huýt sáo
		// 6: Đậu thần / Nhặt đồ
		if (id == 7 || id == 10 || id == 11 || id == 14 || id == 23 || id == 6)
		{
			return true;
		}
		if (s.template.isBuffToPlayer() || s.template.isUseAlone())
		{
			return true;
		}
		return false;
	}

	public static long getManaUse(Skill s)
	{
		if (s == null || s.template == null)
		{
			return 0L;
		}
		if (s.template.manaUseType == 2)
		{
			return 1L;
		}
		if (s.template.manaUseType == 1)
		{
			return s.manaUse * Char.myCharz().cMPFull / 100;
		}
		return s.manaUse;
	}

	// Tự động dùng skill Trị thương (Cứu) hoặc Tái tạo năng lượng khi máu/KI thấp + Tự cắn đậu thần
	private static void checkAutoBuffOrHeal(long now)
	{
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.vSkillFight == null || myChar.statusMe == 14)
		{
			return;
		}

		// 1. TỰ ĐỘNG CẮN ĐẬU THẦN (PEAN) KHI MÁU HOẶC KI TỤT DƯỚI 40%
		// Cực kỳ quan trọng khi dùng Kaioken: Luôn giữ đầy máu/KI, không bao giờ bị tụt về 1 HP gây "treo sát thương 1 điểm"!
		if (myChar.cHP > 0 && (myChar.cHP <= myChar.cHPFull * 45 / 100 || myChar.cMP <= myChar.cMPFull * 30 / 100))
		{
			if (now - lastTimeEatPean >= 1000L)
			{
				lastTimeEatPean = now;
				bool used = myChar.doUsePotion();
				if (!used)
				{
					GameScr.gI().doUseHP();
					Service.gI().requestPean();
				}
			}
		}

		// 2. Nếu máu dưới 45%: Dùng skill Trị thương (Cứu Namếc) nếu người dùng CÓ TÍCH CHỌN
		if (myChar.cHP > 0 && myChar.cHP < myChar.cHPFull * 45 / 100 && selectedSkillIds.Contains(7))
		{
			for (int i = 0; i < myChar.vSkillFight.size(); i++)
			{
				Skill s = (Skill)myChar.vSkillFight.elementAt(i);
				if (s != null && s.template != null && s.template.id == 7)
				{
					if (now - s.lastTimeUseThisSkill >= s.coolDown && myChar.cMP >= getManaUse(s))
					{
						GameScr.gI().doUseSkill(s, true);
						GameScr.isAutoPlay = false;
						return;
					}
				}
			}
		}

		// 3. Nếu KI dưới 15%: Dùng skill Tái tạo năng lượng (Namếc) nếu người dùng CÓ TÍCH CHỌN
		if (myChar.cMP < myChar.cMPFull * 15 / 100 && selectedSkillIds.Contains(10))
		{
			for (int i = 0; i < myChar.vSkillFight.size(); i++)
			{
				Skill s = (Skill)myChar.vSkillFight.elementAt(i);
				if (s != null && s.template != null && s.template.id == 10)
				{
					if (now - s.lastTimeUseThisSkill >= s.coolDown)
					{
						GameScr.gI().doUseSkill(s, true);
						GameScr.isAutoPlay = false;
						return;
					}
				}
			}
		}
	}

	// Kiểm tra xem map đã hết quái chưa
	public static bool isMapOutOfMobs()
	{
		// Nếu map không có quái (nhà, làng, trạm vũ trụ...) -> không đổi khu
		if (GameScr.vMob == null || GameScr.vMob.size() == 0)
		{
			return false;
		}

		// Đếm quái còn sống và có thể đánh được
		for (int i = 0; i < GameScr.vMob.size(); i++)
		{
			Mob m = (Mob)GameScr.vMob.elementAt(i);
			if (m != null && !m.isMobMe && m.status != 0 && m.status != 1 && m.hp > 0)
			{
				return false; // Vẫn còn ít nhất 1 con quái sống
			}
		}

		return true; // Toàn bộ quái trong map đã chết!
	}

	// Logic Tự Động Đổi Khu Khi Hết Quái
	private static void updateAutoChangeZone(long now)
	{
		// Nếu người chơi đang mở UI (menu, chat) hoặc đang mở hành trang: KHÔNG đổi khu
		if (isPlayerBusyInUI() || (GameCanvas.panel != null && GameCanvas.panel.isShow))
		{
			return;
		}

		// Nhận diện khi đã chuyển sang khu mới thành công (zoneID thay đổi)
		if (TileMap.zoneID != currentZoneID)
		{
			currentZoneID = TileMap.zoneID;
			isWaitingZoneData = false;
			lastZoneChangeTime = now;
			Char.myCharz().mobFocus = null;
			return;
		}

		// Nếu trong map vẫn còn quái sống -> reset cờ chờ, không làm gì
		if (!isMapOutOfMobs())
		{
			isWaitingZoneData = false;
			return;
		}

		// ĐÃ HẾT QUÁI TRONG MAP!
		// Tuân thủ cooldown đổi khu 8 giây của server để không bao giờ bị báo chờ giây
		if (now - lastZoneChangeTime < 8000L)
		{
			return;
		}

		// FALLBACK: Nếu đã gửi yêu cầu lấy danh sách khu quá 2500ms mà server chưa phản hồi
		if (isWaitingZoneData && (now - lastZoneRequestTime >= 2500L))
		{
			isWaitingZoneData = false;
			lastZoneChangeTime = now;
			int maxZ = (GameScr.gI().zones != null && GameScr.gI().zones.Length > 0) ? GameScr.gI().zones.Length : 20;
			int nextZ = (TileMap.zoneID + 1) % maxZ;
			notify("Hết quái -> Chuyển sang khu " + nextZ);
			Service.gI().requestChangeZone(nextZ, -1);
			if (GameCanvas.panel != null && GameCanvas.panel.isShow)
			{
				GameCanvas.panel.hide();
			}
			return;
		}

		// Gửi yêu cầu mở danh sách khu để tìm khu 0 người hoặc ít người nhất
		if (!isWaitingZoneData)
		{
			isWaitingZoneData = true;
			lastZoneRequestTime = now;
			lastZoneChangeTime = now;
			Service.gI().openUIZone();
		}
	}

	// Tách danh sách ItemID từ chuỗi nhập vào
	public static List<int> parseFilterIds(string filter)
	{
		List<int> list = new List<int>();
		if (string.IsNullOrEmpty(filter))
		{
			return list;
		}
		string[] parts = filter.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < parts.Length; i++)
		{
			int id;
			if (int.TryParse(parts[i].Trim(), out id))
			{
				if (!list.Contains(id))
				{
					list.Add(id);
				}
			}
		}
		return list;
	}

	// Tự động nhặt vật phẩm & Bùa thu hút (Sử dụng tọa độ chuẩn, hỗ trợ Teleport chớp nhoáng)
	private static void updateAutoPick(long now)
	{
		try
		{
			Char myChar = Char.myCharz();
			if (myChar == null || myChar.statusMe == 14 || TileMap.mapID == -1)
			{
				return;
			}

			if (GameScr.vItemMap == null || GameScr.vItemMap.size() == 0)
			{
				return;
			}

			bool canSendPick = (now - lastPickItemTime >= 200L);
			if (canSendPick)
			{
				lastPickItemTime = now;
			}

			List<int> pickIds = parseFilterIds(pickItemFilter);
			List<int> excludeIds = parseFilterIds(excludeItemFilter);

			int pickCount = 0;
			int total = GameScr.vItemMap.size();
			for (int i = 0; i < total; i++)
			{
				if (i >= GameScr.vItemMap.size())
				{
					break;
				}
				ItemMap item = (ItemMap)GameScr.vItemMap.elementAt(i);
				if (item == null || item.template == null)
				{
					continue;
				}

				// 1. Phân quyền nhặt:
				if (!isPickOthers && pickIds.Count == 0)
				{
					if (item.playerId > 0 && item.playerId != myChar.charID)
					{
						continue;
					}
				}

				// 2. Bộ lọc ID:
				if (pickIds.Count > 0 && !pickIds.Contains(item.template.id))
				{
					continue;
				}
				if (excludeIds.Count > 0 && excludeIds.Contains(item.template.id))
				{
					continue;
				}

				// 3. Gửi lệnh nhặt nếu tính năng Tự Động Nhặt đang BẬT:
				if (isAutoPick && canSendPick && pickCount < 3)
				{
					int itemRealX = (item.xEnd != 0) ? item.xEnd : item.x;
					int itemRealY = (item.yEnd != 0) ? item.yEnd : item.y;
					int distToChar = Res.distance(itemRealX, itemRealY, myChar.cx, myChar.cy);

					if (distToChar <= 60)
					{
						Service.gI().pickItem(item.itemMapID);
						pickCount++;
					}
					else if (isPickAllMap || isAutoPick)
					{
						// Nếu ở xa: Teleport chớp nhoáng nhặt nếu người chơi không đè phím di chuyển tay
						if (!isPlayerManuallyMoving())
						{
							int oldX = myChar.cx;
							int oldY = myChar.cy;
							myChar.cx = itemRealX;
							myChar.cy = itemRealY;
							Service.gI().charMove();
							Service.gI().pickItem(item.itemMapID);
							myChar.cx = oldX;
							myChar.cy = oldY;
							Service.gI().charMove();
							pickCount++;
						}
						else if (isBuaThuHut)
						{
							Service.gI().pickItem(item.itemMapID);
							pickCount++;
						}
					}
					else if (isBuaThuHut)
					{
						Service.gI().pickItem(item.itemMapID);
						pickCount++;
					}
				}
			}
		}
		catch (Exception)
		{
		}
	}

	// Tự động vứt vật phẩm theo D/S ItemID Vứt Bỏ
	private static void updateAutoDrop(long now)
	{
		try
		{
			if (!isAutoDrop)
			{
				return;
			}
			// Không vứt khi đang nhập liệu hoặc có dialog hệ thống
			if (activeInputIndex >= 0 || (ChatTextField.gI() != null && ChatTextField.gI().isShow))
			{
				return;
			}
			if (isWaitingDropConfirm)
			{
				if (now - lastDropItemTime > 3000L)
				{
					isWaitingDropConfirm = false;
				}
				else
				{
					return;
				}
			}
			if (now - lastDropItemTime < 1500L)
			{
				return;
			}

			Char myChar = Char.myCharz();
			if (myChar == null || myChar.arrItemBag == null)
			{
				return;
			}

			List<int> dropIds = parseFilterIds(dropItemFilter);
			if (dropIds.Count == 0)
			{
				return;
			}

			for (int i = 0; i < myChar.arrItemBag.Length; i++)
			{
				Item item = myChar.arrItemBag[i];
				if (item != null && item.template != null && dropIds.Contains(item.template.id))
				{
					sbyte bagIdx = (sbyte)((item.indexUI >= 0) ? item.indexUI : i);
					// Gửi duy nhất yêu cầu vứt (type 1). Server sẽ gửi lại gói -43 để hỏi xác nhận.
					// Controller.cs sẽ tự động xác nhận CÓ (YES) theo chuẩn NRO.
					isWaitingDropConfirm = true;
					Service.gI().useItem(1, 1, bagIdx, -1);
					lastDropItemTime = now;
					break; // Đợi server xử lý xong gói -43
				}
			}
		}
		catch (Exception)
		{
		}
	}

	// Logic Tàn Sát (Có cơ chế KHÓA MỤC TIÊU - không bị loạn quái)
	private static void updateTanSat(long now)
	{
		// Nếu người chơi đang bận trong UI (menu, panel, chat, nói chuyện NPC): KHÔNG can thiệp!
		if (isPlayerBusyInUI())
		{
			return;
		}

		Char myChar = Char.myCharz();
		if (myChar.statusMe == 14)
		{
			return;
		}

		myChar.delayFall = 0;

		// KHÓA MỤC TIÊU: Nếu con quái đang nhắm vẫn còn sống -> giữ nguyên quái đó!
		Mob targetMob = myChar.mobFocus;
		if (targetMob == null || targetMob.isMobMe || targetMob.status == 0 || targetMob.status == 1 || targetMob.hp <= 0)
		{
			targetMob = null;
			myChar.mobFocus = null;

			// Tìm quái mới gần nhất còn sống (bỏ qua mobMe/đệ tử)
			int minDistance = int.MaxValue;
			if (GameScr.vMob != null)
			{
				for (int i = 0; i < GameScr.vMob.size(); i++)
				{
					Mob mob = (Mob)GameScr.vMob.elementAt(i);
					if (mob != null && !mob.isMobMe && mob.status != 0 && mob.status != 1 && mob.hp > 0)
					{
						int dist = Res.abs(mob.x - myChar.cx) + Res.abs(mob.y - myChar.cy);
						if (dist < minDistance)
						{
							minDistance = dist;
							targetMob = mob;
						}
					}
				}
			}
			myChar.mobFocus = targetMob;
		}

		// Nếu có quái sống -> Dịch chuyển đến và tấn công
		if (targetMob != null)
		{
			myChar.mobFocus = targetMob;
			if (myChar.npcFocus == null)
			{
				myChar.charFocus = null;
				myChar.itemFocus = null;
			}

			// Tọa độ đứng gần quái 25px
			int offset = (myChar.cx <= targetMob.x) ? -25 : 25;
			int desiredX = targetMob.x + offset;
			int desiredY = targetMob.y;

			// NẾU NGƯỜI CHƠI ĐANG THAO TÁC DI CHUYỂN BẰNG TAY:
			// KHÔNG can thiệp tọa độ cx, cy của người chơi để người chơi đi lại tự do!
			if (!isPlayerManuallyMoving())
			{
				if (Res.abs(myChar.cx - desiredX) > 20 || Res.abs(myChar.cy - desiredY) > 20)
				{
					myChar.cx = desiredX;
					myChar.cy = desiredY;
					myChar.cdir = (myChar.cx < targetMob.x) ? 1 : -1;
					if (now - lastMoveSendTime >= 300L)
					{
						lastMoveSendTime = now;
						Service.gI().charMove();
					}
				}
				else
				{
					myChar.cdir = (myChar.cx < targetMob.x) ? 1 : -1;
				}
			}
			else
			{
				myChar.cdir = (myChar.cx < targetMob.x) ? 1 : -1;
			}

			long attackDelay = (long)(280f / Mathf.Max(1f, currentSpeed * 0.4f));
			if (attackDelay < 200L) attackDelay = 200L;
			if (now - lastAttackTime >= attackDelay)
			{
				lastAttackTime = now;
				attackTarget(targetMob);
			}
		}
	}

	// Logic Tự Luyện Tập độc lập (Đứng đâu pem đó, không dịch chuyển)
	private static void updateAutoTrain(long now)
	{
		if (isPlayerBusyInUI())
		{
			return;
		}
		Char myChar = Char.myCharz();
		if (myChar.statusMe == 14)
		{
			return;
		}

		myChar.delayFall = 0;
		Mob mob = myChar.mobFocus;

		// Nếu chưa chọn quái, tìm quái gần nhất trong bán kính 200px
		if (mob == null || mob.isMobMe || mob.status == 0 || mob.status == 1 || mob.hp <= 0)
		{
			mob = null;
			myChar.mobFocus = null;
			int minDistance = 200;
			if (GameScr.vMob != null)
			{
				for (int i = 0; i < GameScr.vMob.size(); i++)
				{
					Mob m = (Mob)GameScr.vMob.elementAt(i);
					if (m != null && !m.isMobMe && m.status != 0 && m.status != 1 && m.hp > 0)
					{
						int dist = Res.abs(m.x - myChar.cx) + Res.abs(m.y - myChar.cy);
						if (dist < minDistance)
						{
							minDistance = dist;
							mob = m;
						}
					}
				}
			}
			myChar.mobFocus = mob;
		}

		if (mob != null)
		{
			long attackDelay = (long)(280f / Mathf.Max(1f, currentSpeed * 0.4f));
			if (attackDelay < 200L) attackDelay = 200L;
			if (now - lastAttackTime >= attackDelay)
			{
				lastAttackTime = now;
				attackTarget(mob);
			}
		}
	}

	// Đánh quái: CHỈ DÙNG các skill TẤN CÔNG (Bỏ qua skill Cứu/Trị thương/Hồi KI)
	private static void attackTarget(Mob mob)
	{
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.vSkillFight == null || myChar.vSkillFight.size() == 0 || mob == null)
		{
			return;
		}

		long now = mSystem.currentTimeMillis();

		// Nếu nhân vật đang bị kẹt animation hoặc đang trỏ vào skill hỗ trợ/cứu -> hủy ngay để đánh quái
		if (myChar.isSelectingSkillBuffToPlayer() || myChar.isSelectingSkillUseAlone())
		{
			myChar.skillPaint = null;
		}

		Skill skillToUse = null;

		// 1. NẾU NGƯỜI DÙNG ĐÃ TÍCH CHỌN SKILL TRONG BẢNG:
		// CHỈ DÙNG DUY NHẤT CÁC SKILL ĐÃ TÍCH CHỌN! (Tuyệt đối không tự ý dùng skill khác)
		if (selectedSkillIds.Count > 0)
		{
			for (int i = 0; i < myChar.vSkillFight.size(); i++)
			{
				Skill s = (Skill)myChar.vSkillFight.elementAt(i);
				if (s != null && s.template != null && selectedSkillIds.Contains(s.template.id))
				{
					// Bỏ qua skill buff nếu đang đánh quái
					if (isBuffOrSupportSkill(s))
					{
						continue;
					}

					// Kiểm tra hồi chiêu và MP
					if (now - s.lastTimeUseThisSkill >= s.coolDown && myChar.cMP >= getManaUse(s))
					{
						skillToUse = s;
						break;
					}
				}
			}

			// Nếu các skill tích chọn đều đang hồi chiêu hoặc chưa sẵn sàng:
			// Đứng đợi hồi chiêu đúng theo ý người dùng, KHÔNG TỰ Ý ĐỔI SANG SKILL KHÁC!
			if (skillToUse == null)
			{
				return;
			}
		}
		// 2. NẾU KHÔNG TÍCH CHỌN SKILL NÀO (Bảng trống):
		// Mặc định chỉ dùng đòn đấm cơ bản (index 0)
		else if (myChar.vSkillFight.size() > 0)
		{
			Skill s0 = (Skill)myChar.vSkillFight.elementAt(0);
			if (s0 != null && s0.template != null && !isBuffOrSupportSkill(s0))
			{
				skillToUse = s0;
			}
		}

		if (skillToUse != null)
		{
			// Khóa chặt mục tiêu vào quái và quay mặt về phía quái
			myChar.mobFocus = mob;
			myChar.charFocus = null;
			myChar.itemFocus = null;
			myChar.cdir = (myChar.cx < mob.x) ? 1 : -1;

			// Bắn đòn chuẩn bằng doUseSkill (doUseSkill tự đồng bộ selectSkill sạch sẽ)
			GameScr.gI().doUseSkill(skillToUse, true);
			GameScr.isAutoPlay = false;
		}
	}

	// Callback khi nhận danh sách khu từ Server
	public static void onReceiveZones()
	{
		if (isAutoChangeZone && isWaitingZoneData)
		{
			isWaitingZoneData = false;
			lastZoneChangeTime = mSystem.currentTimeMillis();

			int[] zones = GameScr.gI().zones;
			int[] numPlayer = GameScr.gI().numPlayer;
			int[] maxPlayer = GameScr.gI().maxPlayer;

			if (zones != null && zones.Length > 0)
			{
				int bestZone = -1;
				int minPlayerCount = int.MaxValue;

				for (int i = 0; i < zones.Length; i++)
				{
					int zId = zones[i];
					if (zId == -1 || zId == TileMap.zoneID)
					{
						continue;
					}

					int players = (numPlayer != null && i < numPlayer.Length) ? numPlayer[i] : 0;
					int maxP = (maxPlayer != null && i < maxPlayer.Length) ? maxPlayer[i] : 20;

					// Chỉ chọn khu chưa đầy
					if (players < maxP)
					{
						if (players < minPlayerCount)
						{
							minPlayerCount = players;
							bestZone = zId;
							if (players == 0)
							{
								break; // Ưu tiên tuyệt đối khu 0 người!
							}
						}
					}
				}

				if (bestZone == -1)
				{
					bestZone = (TileMap.zoneID + 1) % zones.Length;
				}

				notify("Hết quái -> Chuyển sang khu " + bestZone + " (" + (minPlayerCount == int.MaxValue ? 0 : minPlayerCount) + " người)");
				Service.gI().requestChangeZone(bestZone, -1);
			}
			else
			{
				int nextZ = (TileMap.zoneID + 1) % 20;
				notify("Hết quái -> Chuyển sang khu " + nextZ);
				Service.gI().requestChangeZone(nextZ, -1);
			}

			if (GameCanvas.panel != null && GameCanvas.panel.isShow)
			{
				GameCanvas.panel.hide();
			}
		}
	}

	// =============================================================
	// KIỂM TRA HIỆU LỰC BUFF TRÊN NHÂN VẬT (ICON / TEXT)
	// =============================================================
	// QUẢN LÝ BUFF ITEM & THỨC ĂN (KHÔNG BỊ DELAY, ĂN CHUẨN KHI HẾT HIỆU LỰC)
	// =============================================================
	public static string removeAccents(string text)
	{
		if (string.IsNullOrEmpty(text)) return "";
		string[] s1 = new string[] { "à", "á", "ạ", "ả", "ã", "â", "ầ", "ấ", "ậ", "ẩ", "ẫ", "ă", "ằ", "ắ", "ặ", "ẳ", "ẵ" };
		string[] s2 = new string[] { "è", "é", "ẹ", "ẻ", "ẽ", "ê", "ề", "ế", "ệ", "ể", "ễ" };
		string[] s3 = new string[] { "ì", "í", "ị", "ỉ", "ĩ" };
		string[] s4 = new string[] { "ò", "ó", "ọ", "ỏ", "õ", "ô", "ồ", "ố", "ộ", "ổ", "ỗ", "ơ", "ờ", "ớ", "ợ", "ở", "ỡ" };
		string[] s5 = new string[] { "ù", "ú", "ụ", "ủ", "ũ", "ư", "ừ", "ứ", "ự", "ử", "ữ" };
		string[] s6 = new string[] { "ỳ", "ý", "ỵ", "ỷ", "ỹ" };
		string[] s7 = new string[] { "đ" };
		text = text.ToLower();
		for (int i = 0; i < s1.Length; i++) text = text.Replace(s1[i], "a");
		for (int i = 0; i < s2.Length; i++) text = text.Replace(s2[i], "e");
		for (int i = 0; i < s3.Length; i++) text = text.Replace(s3[i], "i");
		for (int i = 0; i < s4.Length; i++) text = text.Replace(s4[i], "o");
		for (int i = 0; i < s5.Length; i++) text = text.Replace(s5[i], "u");
		for (int i = 0; i < s6.Length; i++) text = text.Replace(s6[i], "y");
		for (int i = 0; i < s7.Length; i++) text = text.Replace(s7[i], "d");
		return text;
	}

	public static void onItemTimeBuffReceived(short idIcon, int seconds)
	{
		long now = mSystem.currentTimeMillis();
		if (lastUsedItemTemplateId > 0 && now - lastUsedItemSendTime < 2500L)
		{
			itemBuffIconMap[lastUsedItemTemplateId] = idIcon;
			if (isFoodItem("", lastUsedItemTemplateId) || seconds == 600 || seconds == 599)
			{
				lastFoodBuffIconId = idIcon;
			}
		}
		else if (seconds == 600 || seconds == 599)
		{
			lastFoodBuffIconId = idIcon;
		}
	}

	public static bool isFoodItem(string name, int templateId = -1)
	{
		if (templateId >= 638 && templateId <= 646) return true;
		if (templateId >= 663 && templateId <= 667) return true;
		if (templateId == 648 || templateId == 649) return true;
		if (string.IsNullOrEmpty(name)) return false;
		string n = removeAccents(name);
		return n.Contains("pudding") || n.Contains("xuc xich")
			|| n.Contains("kem dau") || n.Contains("mi ly")
			|| n.Contains("sushi") || n.Contains("cua rang") || n.Contains("tom")
			|| n.Contains("bach tuoc") || n.Contains("ga quay")
			|| n.Contains("thuc an") || n.Contains("do an");
	}

	public static bool isFoodBuffActive()
	{
		if (Char.vItemTime == null) return false;

		// 1. Nếu đã nhận diện icon buff thức ăn cụ thể (từ packet -106 hoặc icon hiện hành)
		if (lastFoodBuffIconId > 0)
		{
			if (isBuffTimerActive(lastFoodBuffIconId))
			{
				return true;
			}
			// Icon thức ăn cụ thể đã biến mất khỏi thanh buff -> Buff thức ăn đã hết!
			return false;
		}

		// 2. Quét Char.vItemTime nếu chưa có idIcon cụ thể
		for (int i = 0; i < Char.vItemTime.size(); i++)
		{
			ItemTime t = (ItemTime)Char.vItemTime.elementAt(i);
			if (t != null)
			{
				int id = t.idIcon;

				// Thức ăn chỉ kéo dài tối đa 10 phút. Bỏ qua tất cả các buff bùa/luyện tập dài hạn (> 10 phút)
				if (t.minute > 10)
				{
					continue;
				}

				// Bỏ qua các icon đặc thù của Máy Luyện Tập hoặc Bùa
				if (id == 4387 || id == 4028 || id == 521 || id == 721 || id == 4330 || id == 14)
				{
					continue;
				}

				// Bỏ qua các potion bổ trợ khác (Cuồng nộ, Bổ huyết, Bổ khí, Giáp Xên, Sát thương chuẩn)
				bool isKnownPotion = (id >= 2754 && id <= 2763) || (id >= 10714 && id <= 10717);
				if (isKnownPotion)
				{
					continue;
				}

				// Nếu còn thời gian (> 1s) -> Đang có buff thức ăn hoạt động!
				if (t.minute > 0 || t.second > 1)
				{
					lastFoodBuffIconId = (short)id; // Ghi nhớ icon buff thức ăn
					return true;
				}
			}
		}
		return false;
	}

	public static bool isBuffTimerActive(int iconId)
	{
		if (ItemTime.isExistItem(iconId))
		{
			ItemTime itemTime = ItemTime.getItemById(iconId);
			if (itemTime != null && (itemTime.minute > 0 || itemTime.second > 2))
			{
				return true;
			}
		}
		return false;
	}

	public static bool isItemBuffActive(string itemName, Item it)
	{
		try
		{
			string name = itemName.ToLower();
			int templateId = (it != null && it.template != null) ? it.template.id : -1;

			// 0. Kiểm tra nếu đã ánh xạ được buff icon từ packet -106
			if (templateId > 0 && itemBuffIconMap.ContainsKey(templateId))
			{
				short buffIcon = itemBuffIconMap[templateId];
				if (isBuffTimerActive(buffIcon))
				{
					return true;
				}
			}

			// 1. Thức ăn: kiểm tra chung qua isFoodBuffActive() (tránh cắn dồn nhiều thức ăn)
			if (isFoodItem(name, templateId))
			{
				return isFoodBuffActive();
			}

			// 2. Tự động luyện tập: kiểm tra GameScr.textTime hoặc vItemTime
			if (name.Contains("luyện tập") || name.Contains("luyen tap"))
			{
				return isAutoTrainBuffActive();
			}

			// 3. Cuồng nộ
			if (name.Contains("cuồng nộ 2") || name.Contains("cuong no 2"))
			{
				return isBuffTimerActive(2758) || isBuffTimerActive(10714);
			}
			if (name.Contains("cuồng nộ") || name.Contains("cuong no"))
			{
				return isBuffTimerActive(2754) || isBuffTimerActive(2758) || isBuffTimerActive(10714);
			}

			// 4. Bổ huyết
			if (name.Contains("bổ huyết 2") || name.Contains("bo huyet 2"))
			{
				return isBuffTimerActive(2759) || isBuffTimerActive(10715);
			}
			if (name.Contains("bổ huyết") || name.Contains("bo huyet"))
			{
				return isBuffTimerActive(2755) || isBuffTimerActive(2759) || isBuffTimerActive(10715);
			}

			// 5. Bổ khí
			if (name.Contains("bổ khí 2") || name.Contains("bo khi 2"))
			{
				return isBuffTimerActive(2760) || isBuffTimerActive(10716);
			}
			if (name.Contains("bổ khí") || name.Contains("bo khi"))
			{
				return isBuffTimerActive(2756) || isBuffTimerActive(2760) || isBuffTimerActive(10716);
			}

			// 6. Giáp Xên
			if ((name.Contains("giáp xên") || name.Contains("giap xen")) && (name.Contains("2") || name.Contains("cấp 2")))
			{
				return isBuffTimerActive(2761) || isBuffTimerActive(10717);
			}
			if (name.Contains("giáp xên") || name.Contains("giap xen"))
			{
				return isBuffTimerActive(2757) || isBuffTimerActive(2761) || isBuffTimerActive(10717);
			}

			// 7. Sát thương chuẩn
			if (name.Contains("sát thương chuẩn") || name.Contains("sat thuong chuan"))
			{
				if (name.Contains("2") || name.Contains("cấp 2"))
				{
					return isBuffTimerActive(2763);
				}
				return isBuffTimerActive(2762) || isBuffTimerActive(2763);
			}

			// 8. Ẩn danh
			if (name.Contains("ẩn danh") || name.Contains("an danh"))
			{
				if (name.Contains("2"))
				{
					return isBuffTimerActive(4388);
				}
				return isBuffTimerActive(4387) || isBuffTimerActive(4388);
			}

			// 9. Nho tím / Nho xanh (hồi thể lực)
			if (name.Contains("nho"))
			{
				Char myChar = Char.myCharz();
				if (myChar != null && myChar.cStamina >= 80)
				{
					return true;
				}
				return false;
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	// Kiểm tra xem Bùa / Buff / Giáp Tự Động Luyện Tập có đang hoạt động hay không
	public static bool isAutoTrainBuffActive()
	{
		return hasTrainingGearOrBuff();
	}

	// Tìm item Tự Động Luyện Tập trong hành trang
	public static Item findAutoTrainItemInBag()
	{
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.arrItemBag == null) return null;

		for (int i = 0; i < myChar.arrItemBag.Length; i++)
		{
			Item it = myChar.arrItemBag[i];
			if (it != null && it.template != null)
			{
				if (it.template.id == 521)
				{
					return it;
				}
				if (!string.IsNullOrEmpty(it.template.name))
				{
					string name = it.template.name.ToLower();
					if (name.Contains("luyện tập") || name.Contains("luyen tap") || name.Contains("tập luyện") || name.Contains("máy luyện"))
					{
						return it;
					}
				}
			}
		}
		return null;
	}

	public static bool isAnyFoodSelected()
	{
		for (int i = 5; i <= 8; i++)
		{
			if (itemCol1Selected[i] || itemCol2Selected[i]) return true;
		}
		return false;
	}

	public static Item findFoodInBag()
	{
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.arrItemBag == null) return null;

		// 1. Ưu tiên tìm các món thức ăn được tích chọn trong Cột 1 & Cột 2
		for (int i = 5; i <= 8; i++)
		{
			if (itemCol1Selected[i])
			{
				Item it = findItemInBag(itemNamesCol1[i]);
				if (it != null) return it;
			}
			if (itemCol2Selected[i])
			{
				Item it = findItemInBag(itemNamesCol2[i]);
				if (it != null) return it;
			}
		}

		// 2. Nếu đã tích chọn món thức ăn nào đó nhưng các món ưu tiên không còn, quét bất kỳ món thức ăn nào trong túi
		if (isAnyFoodSelected())
		{
			for (int i = 0; i < myChar.arrItemBag.Length; i++)
			{
				Item it = myChar.arrItemBag[i];
				if (it != null && it.template != null)
				{
					if (isFoodItem(it.template.name, it.template.id))
					{
						return it;
					}
				}
			}
		}

		return null;
	}

	// =============================================================
	// LOGIC AUTO ITEM (ĂN LẠI NGAY LẬP TỨC KHI HẾT HIỆU LỰC BUFF)
	// =============================================================
	private static void updateAutoItem(long now)
	{
		// Quét mỗi 500ms để kịp thời ăn ngay khi item hết hạn (không delay)
		if (now - lastTimeCheckItem < 500L)
		{
			return;
		}
		lastTimeCheckItem = now;

		Char myChar = Char.myCharz();
		if (myChar == null || myChar.arrItemBag == null || myChar.statusMe == 14)
		{
			return;
		}

		long safeCooldown = 3000L; // Giãn cách tối thiểu 3s giữa các lần dùng

		// -------------------------------------------------------------
		// PHẦN 1: TỰ ĐỘNG ĂN THỨC ĂN (ĂN NGAY KHI HẾT HIỆU LỰC, KHÔNG DELAY)
		// -------------------------------------------------------------
		if (isAnyFoodSelected())
		{
			bool hasFoodBuff = isFoodBuffActive();
			if (!hasFoodBuff && (now - lastTimeEatFood >= safeCooldown))
			{
				Item food = findFoodInBag();
				if (food != null)
				{
					lastTimeEatFood = now;
					useItem(food);
					return;
				}
			}
		}

		// -------------------------------------------------------------
		// PHẦN 2: QUÉT CÁC POTION & ITEM BỔ TRỢ (CỘT 1 & CỘT 2)
		// -------------------------------------------------------------
		// Quét Cột 1
		for (int i = 0; i < itemNamesCol1.Length; i++)
		{
			// Bỏ qua thức ăn vì đã được xử lý chuyên biệt ở PHẦN 1
			if (i >= 5 && i <= 8) continue;

			if (itemCol1Selected[i])
			{
				// Xử lý riêng Nho tím (hồi thể lực)
				if (i == 10)
				{
					if (myChar.cStamina < 20 && (now - lastTimeUsedCol1[i] >= safeCooldown))
					{
						Item nho = findItemInBag(itemNamesCol1[i]);
						if (nho != null)
						{
							useItem(nho);
							lastTimeUsedCol1[i] = now;
							return;
						}
					}
					continue;
				}

				// Xử lý riêng Tự Động Luyện Tập (i == 11)
				if (i == 11)
				{
					bool hasBuff = isAutoTrainBuffActive();
					if (!hasBuff && (now - lastTimeUsedCol1[i] >= safeCooldown))
					{
						Item trainItem = findAutoTrainItemInBag();
						if (trainItem != null)
						{
							useItem(trainItem);
							lastTimeUsedCol1[i] = now;
							notify("Auto dùng: " + trainItem.template.name);
							return;
						}
					}
					continue;
				}

				Item it = findItemInBag(itemNamesCol1[i]);
				if (it != null)
				{
					bool isActive = isItemBuffActive(itemNamesCol1[i], it);
					if (!isActive && (now - lastTimeUsedCol1[i] >= safeCooldown))
					{
						useItem(it);
						lastTimeUsedCol1[i] = now;
						return;
					}
				}
			}
		}

		// Quét Cột 2
		for (int i = 0; i < itemNamesCol2.Length; i++)
		{
			// Bỏ qua thức ăn vì đã được xử lý chuyên biệt ở PHẦN 1
			if (i >= 5 && i <= 8) continue;

			if (itemCol2Selected[i])
			{
				// Xử lý riêng Nho xanh (hồi thể lực)
				if (i == 10)
				{
					if (myChar.cStamina < 20 && (now - lastTimeUsedCol2[i] >= safeCooldown))
					{
						Item nho = findItemInBag(itemNamesCol2[i]);
						if (nho != null)
						{
							useItem(nho);
							lastTimeUsedCol2[i] = now;
							return;
						}
					}
					continue;
				}
				// Xử lý Bùa x2 đệ tử
				if (i == 11)
				{
					if (now - lastTimeUsedCol2[i] >= 3600000L) // 1 tiếng
					{
						Item bua = findItemInBag(itemNamesCol2[i]);
						if (bua != null)
						{
							useItem(bua);
							lastTimeUsedCol2[i] = now;
							return;
						}
					}
					continue;
				}

				Item it = findItemInBag(itemNamesCol2[i]);
				if (it != null)
				{
					bool isActive = isItemBuffActive(itemNamesCol2[i], it);
					if (!isActive && (now - lastTimeUsedCol2[i] >= safeCooldown))
					{
						useItem(it);
						lastTimeUsedCol2[i] = now;
						return;
					}
				}
			}
		}

		// -------------------------------------------------------------
		// PHẦN 3: D/S ITEMID TỰ DÙNG NHẬP TAY (eatItemFilter)
		// -------------------------------------------------------------
		List<int> eatIds = parseFilterIds(eatItemFilter);
		if (eatIds.Count > 0)
		{
			for (int i = 0; i < myChar.arrItemBag.Length; i++)
			{
				Item it = myChar.arrItemBag[i];
				if (it != null && it.template != null && eatIds.Contains(it.template.id))
				{
					if (it.template.id == 6 || it.template.name.ToLower().Contains("đậu thần") || it.template.name.ToLower().Contains("dau than"))
					{
						if (myChar.cHP <= myChar.cHPFull * 70 / 100 || myChar.cMP <= myChar.cMPFull * 50 / 100)
						{
							if (now - lastTimeEatPean >= 1200L)
							{
								lastTimeEatPean = now;
								useItem(it);
								return;
							}
						}
					}
					else if (isFoodItem(it.template.name, it.template.id))
					{
						if (!isFoodBuffActive() && (now - lastTimeEatFood >= safeCooldown))
						{
							lastTimeEatFood = now;
							useItem(it);
							return;
						}
					}
					else
					{
						int itemKey = it.template.id;
						bool isActive = isItemBuffActive(it.template.name, it);
						if (isActive)
						{
							continue;
						}

						long lastUsedTime = 0L;
						if (lastTimeUsedCustomItems.ContainsKey(itemKey))
						{
							lastUsedTime = lastTimeUsedCustomItems[itemKey];
						}

						if (lastUsedTime == 0L || (now - lastUsedTime >= 600000L))
						{
							useItem(it);
							lastTimeUsedCustomItems[itemKey] = now;
							return;
						}
					}
				}
			}
		}
	}

	private static Item findItemInBag(string nameKeyword)
	{
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.arrItemBag == null) return null;

		string kw = removeAccents(nameKeyword.Trim());
		bool isLevel2 = kw.Contains(" 2") || kw.EndsWith("2");
		string baseKw = isLevel2 ? kw.Replace(" 2", "").Trim() : kw;

		// 1. Quét nhanh theo ID template cho các món thức ăn phổ biến (chính xác 100%)
		int targetFoodId = -1;
		if (baseKw.Contains("pudding")) targetFoodId = 638;
		else if (baseKw.Contains("xuc xich")) targetFoodId = 639;
		else if (baseKw.Contains("kem dau")) targetFoodId = 640;
		else if (baseKw.Contains("mi ly")) targetFoodId = 641;
		else if (baseKw.Contains("sushi")) targetFoodId = 642;
		else if (baseKw.Contains("cua rang")) targetFoodId = 643;
		else if (baseKw.Contains("bach tuoc")) targetFoodId = 644;
		else if (baseKw.Contains("tom")) targetFoodId = 645;
		else if (baseKw.Contains("ga quay")) targetFoodId = 646;

		if (targetFoodId > 0)
		{
			for (int i = 0; i < myChar.arrItemBag.Length; i++)
			{
				Item it = myChar.arrItemBag[i];
				if (it != null && it.template != null && it.template.id == targetFoodId)
				{
					return it;
				}
			}
		}

		// 2. Quét theo tên (đã chuẩn hóa không dấu)
		for (int i = 0; i < myChar.arrItemBag.Length; i++)
		{
			Item it = myChar.arrItemBag[i];
			if (it != null && it.template != null && !string.IsNullOrEmpty(it.template.name))
			{
				string itemName = removeAccents(it.template.name);
				if (!itemName.Contains(baseKw))
				{
					continue;
				}
				bool itemIsLevel2 = itemName.Contains(" 2") || itemName.Contains("cap 2") || itemName.EndsWith("2");
				if (isLevel2 == itemIsLevel2)
				{
					return it;
				}
			}
		}
		return null;
	}

	public static int findItemBagIndex(Item item)
	{
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.arrItemBag == null || item == null) return -1;
		for (int i = 0; i < myChar.arrItemBag.Length; i++)
		{
			if (myChar.arrItemBag[i] == item)
			{
				return i;
			}
		}
		return item.indexUI;
	}

	private static void useItem(Item item)
	{
		if (item != null)
		{
			int bagIdx = findItemBagIndex(item);
			if (bagIdx >= 0)
			{
				lastUsedItemTemplateId = (item.template != null) ? item.template.id : -1;
				lastUsedItemSendTime = mSystem.currentTimeMillis();
				Service.gI().useItem(0, 1, (sbyte)bagIdx, -1);
				notify("Auto dùng: " + item.template.name);
			}
		}
	}

	// -------------------------------------------------------------
	// GIAO DIỆN VẼ LÊN MÀN HÌNH (HUD & 3 BẢNG CÀI ĐẶT POPUP)
	// -------------------------------------------------------------
	public static void paint(mGraphics g)
	{
		if (Char.myCharz() == null || TileMap.mapID == -1)
		{
			return;
		}

		// 1. Nút mở bảng cài đặt mod nhỏ gọn [⚙ Menu (F2)] (Gọn gàng, không che màn hình)
		int btnX = 6;
		int btnY = 45;
		int btnW = 85;
		int btnH = 16;
		g.setColor(1800936);
		g.fillRect(btnX, btnY, btnW, btnH);
		g.setColor(2758924);
		g.drawRect(btnX, btnY, btnW, btnH);
		mFont.tahoma_7b_white.drawString(g, "⚙ Menu (F2)", btnX + btnW / 2, btnY + 2, mFont.CENTER);

		// Nếu người dùng chủ động tích chọn "Hiện bảng HUD màn hình" thì mới vẽ thêm khung chữ
		if (isShowHUD)
		{
			int x = 6;
			int y = btnY + btnH + 3;
			int lineH = 11;

			g.setColor(0, 0.45f);
			g.fillRect(x - 3, y - 3, 140, 94);

			mFont font = mFont.tahoma_7_white;
			mFont fontYellow = mFont.tahoma_7_yellow;

			fontYellow.drawString(g, "[MOD NRO - AUTO]", x, y, 0);
			font.drawString(g, "Tàn sát: " + (isTanSat ? "BẬT" : "TẮT") + " ('ts')", x, y + lineH, 0);
			font.drawString(g, "Tự luyện tập: " + (isAutoTrain ? "BẬT" : "TẮT") + " ('ak')", x, y + lineH * 2, 0);
			font.drawString(g, "Đổi khu hết quái: " + (isAutoChangeZone ? "BẬT" : "TẮT") + " ('ck')", x, y + lineH * 3, 0);
			font.drawString(g, "Auto Item: " + (isAutoItem ? "BẬT" : "TẮT") + " ('item')", x, y + lineH * 4, 0);
			font.drawString(g, "Tự nhặt: " + (isAutoPick ? "BẬT" : "TẮT") + " ('nhat')", x, y + lineH * 5, 0);
			font.drawString(g, "Bùa thu hút: " + (isBuaThuHut ? "BẬT" : "TẮT") + " ('bua')", x, y + lineH * 6, 0);
			font.drawString(g, "Tốc độ: x" + currentSpeed + " ('s1'..'s10')", x, y + lineH * 7, 0);
		}

		// 2. BẢNG CÀI ĐẶT CHÍNH (MAIN CONTROL PANEL)
		if (isShowMainDialog)
		{
			paintMainControlDialog(g);
		}

		// 3. BẢNG SELECT SKILL (HÌNH 3)
		if (isShowSkillDialog)
		{
			paintSkillDialog(g);
		}

		// 4. BẢNG AUTO ITEM (HÌNH 1 & 2)
		if (isShowItemDialog)
		{
			paintItemDialog(g);
		}

		// 5. BẢNG AUTO NHẶT (HÌNH ẢNH MẪU YÊU CẦU)
		if (isShowPickDialog)
		{
			paintPickDialog(g);
		}
	}

	// =============================================================
	// 1. VẼ BẢNG CÀI ĐẶT CHÍNH (MAIN CONTROL PANEL)
	// =============================================================
	private static void paintMainControlDialog(mGraphics g)
	{
		int boxW = 200;
		int boxH = 295;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;
		int headerH = 24;

		// Nền vàng cát
		g.setColor(14860437);
		g.fillRect(boxX, boxY, boxW, boxH);
		g.setColor(2758924);
		g.drawRect(boxX, boxY, boxW, boxH);

		// Tiêu đề xanh
		g.setColor(1800936);
		g.fillRect(boxX, boxY, boxW, headerH);
		mFont.tahoma_7b_white.drawString(g, "BẢNG CÀI ĐẶT MOD", boxX + boxW / 2, boxY + 5, mFont.CENTER);

		// Nút [X] đóng ở góc trên
		int btnCloseX = boxX + boxW - 20;
		int btnCloseY = boxY + 3;
		g.setColor(13382451);
		g.fillRect(btnCloseX, btnCloseY, 16, 16);
		g.setColor(16777215);
		g.drawRect(btnCloseX, btnCloseY, 16, 16);
		mFont.tahoma_7b_white.drawString(g, "X", btnCloseX + 8, btnCloseY + 2, mFont.CENTER);

		// Các ô tích chọn tính năng chính (7 dòng)
		int startY = boxY + headerH + 6;
		int rowH = 18;

		drawCheckboxRow(g, boxX + 10, startY, "Tàn sát quái trong map", isTanSat);
		drawCheckboxRow(g, boxX + 10, startY + rowH, "Tự động luyện tập (ak)", isAutoTrain);
		drawCheckboxRow(g, boxX + 10, startY + rowH * 2, "Auto đổi khu khi hết quái", isAutoChangeZone);
		drawCheckboxRow(g, boxX + 10, startY + rowH * 3, "Tự động nhặt vật phẩm", isAutoPick);
		drawCheckboxRow(g, boxX + 10, startY + rowH * 4, "Bùa thu hút (Hút đồ)", isBuaThuHut);
		drawCheckboxRow(g, boxX + 10, startY + rowH * 5, "Auto Item (Đồ ăn, Cuồng nộ)", isAutoItem);
		drawCheckboxRow(g, boxX + 10, startY + rowH * 6, "Hiện bảng HUD màn hình", isShowHUD);

		// 3 Nút mở bảng phụ: [ Skill ] [ Item ] [ Nhặt ]
		int btnSubY = startY + rowH * 7 + 4;
		int btnSubW = 57;

		// 1. Skill
		g.setColor(1800936);
		g.fillRect(boxX + 10, btnSubY, btnSubW, 18);
		g.drawRect(boxX + 10, btnSubY, btnSubW, 18);
		mFont.tahoma_7b_white.drawString(g, "🎯 Skill", boxX + 10 + btnSubW / 2, btnSubY + 3, mFont.CENTER);

		// 2. Item
		g.setColor(1800936);
		g.fillRect(boxX + 71, btnSubY, btnSubW, 18);
		g.drawRect(boxX + 71, btnSubY, btnSubW, 18);
		mFont.tahoma_7b_white.drawString(g, "🍗 Item", boxX + 71 + btnSubW / 2, btnSubY + 3, mFont.CENTER);

		// 3. Auto Nhặt
		g.setColor(1800936);
		g.fillRect(boxX + 132, btnSubY, 58, 18);
		g.drawRect(boxX + 132, btnSubY, 58, 18);
		mFont.tahoma_7b_white.drawString(g, "📦 Nhặt", boxX + 132 + 29, btnSubY + 3, mFont.CENTER);

		// Thanh chọn tốc độ game (Cheat Speed)
		int speedY = btnSubY + 23;
		mFont.tahoma_7b_dark.drawString(g, "Tốc độ: x" + currentSpeed, boxX + 12, speedY, 0);

		// Nút [-] và [+] bên phải tiêu đề tốc độ
		int btnMiniW = 20;
		int btnMiniH = 14;
		int btnMinusX = boxX + 140;
		int btnPlusX = boxX + 168;

		g.setColor(1800936);
		g.fillRect(btnMinusX, speedY - 1, btnMiniW, btnMiniH);
		g.setColor(2758924);
		g.drawRect(btnMinusX, speedY - 1, btnMiniW, btnMiniH);
		mFont.tahoma_7b_white.drawString(g, "-", btnMinusX + btnMiniW / 2, speedY - 1, mFont.CENTER);

		g.setColor(1800936);
		g.fillRect(btnPlusX, speedY - 1, btnMiniW, btnMiniH);
		g.setColor(2758924);
		g.drawRect(btnPlusX, speedY - 1, btnMiniW, btnMiniH);
		mFont.tahoma_7b_white.drawString(g, "+", btnPlusX + btnMiniW / 2, speedY - 1, mFont.CENTER);

		// 2 HÀNG NÚT TỐC ĐỘ (1x -> 10x)
		float[] speeds = new float[] { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f };
		string[] speedLabels = new string[] { "1x", "2x", "3x", "4x", "5x", "6x", "7x", "8x", "9x", "10x" };
		int btnSpdW = 32;
		int btnSpdH = 15;
		int gap = 5;
		int startBtnSpdX = boxX + 10;
		int btnSpdY1 = speedY + 14;
		int btnSpdY2 = speedY + 31;

		// Hàng 1 (1x -> 5x)
		for (int i = 0; i < 5; i++)
		{
			int curBtnX = startBtnSpdX + i * (btnSpdW + gap);
			bool isCur = Mathf.Abs(currentSpeed - speeds[i]) < 0.05f;
			g.setColor(isCur ? 3058980 : 1800936);
			g.fillRect(curBtnX, btnSpdY1, btnSpdW, btnSpdH);
			g.setColor(2758924);
			g.drawRect(curBtnX, btnSpdY1, btnSpdW, btnSpdH);
			mFont.tahoma_7b_white.drawString(g, speedLabels[i], curBtnX + btnSpdW / 2, btnSpdY1 + 1, mFont.CENTER);
		}

		// Hàng 2 (6x -> 10x)
		for (int j = 5; j < 10; j++)
		{
			int curBtnX = startBtnSpdX + (j - 5) * (btnSpdW + gap);
			bool isCur = Mathf.Abs(currentSpeed - speeds[j]) < 0.05f;
			g.setColor(isCur ? 3058980 : 1800936);
			g.fillRect(curBtnX, btnSpdY2, btnSpdW, btnSpdH);
			g.setColor(2758924);
			g.drawRect(curBtnX, btnSpdY2, btnSpdW, btnSpdH);
			mFont.tahoma_7b_white.drawString(g, speedLabels[j], curBtnX + btnSpdW / 2, btnSpdY2 + 1, mFont.CENTER);
		}

		// Nút [ ĐÓNG BẢNG (ESC) ] to rõ dưới đáy bảng
		int btnCloseBottomW = 140;
		int btnCloseBottomH = 18;
		int btnCloseBottomX = boxX + (boxW - btnCloseBottomW) / 2;
		int btnCloseBottomY = btnSpdY2 + 20;
		g.setColor(13382451);
		g.fillRect(btnCloseBottomX, btnCloseBottomY, btnCloseBottomW, btnCloseBottomH);
		g.setColor(2758924);
		g.drawRect(btnCloseBottomX, btnCloseBottomY, btnCloseBottomW, btnCloseBottomH);
		mFont.tahoma_7b_white.drawString(g, "✕ ĐÓNG BẢNG (ESC)", btnCloseBottomX + btnCloseBottomW / 2, btnCloseBottomY + 3, mFont.CENTER);
	}

	// =============================================================
	// 2. VẼ BẢNG AUTO ITEM CHUẨN 2 CỘT (HÌNH 1 & 2)
	// =============================================================
	private static void paintItemDialog(mGraphics g)
	{
		int boxW = 275;
		int boxH = 245;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;
		int headerH = 24;

		// Nền vàng cát
		g.setColor(14860437);
		g.fillRect(boxX, boxY, boxW, boxH);
		g.setColor(2758924);
		g.drawRect(boxX, boxY, boxW, boxH);

		// Tiêu đề xanh: Auto Item
		g.setColor(1800936);
		g.fillRect(boxX, boxY, boxW, headerH);
		mFont.tahoma_7b_white.drawString(g, "Auto Item", boxX + boxW / 2, boxY + 5, mFont.CENTER);

		// Nút [X] đóng
		int btnCloseX = boxX + boxW - 17;
		int btnCloseY = boxY + 4;
		g.setColor(13382451);
		g.fillRect(btnCloseX, btnCloseY, 13, 14);
		mFont.tahoma_7b_white.drawString(g, "X", btnCloseX + 7, btnCloseY + 1, mFont.CENTER);

		// 2 CỘT ITEMS CHUẨN THEO HÌNH
		int maxVisibleRows = 8;
		int rowH = 17;
		int startY = boxY + headerH + 5;

		int col1X = boxX + 8;
		int col2X = boxX + 138;

		for (int i = 0; i < maxVisibleRows && (scrollItemIndex + i) < 12; i++)
		{
			int idx = scrollItemIndex + i;
			int curY = startY + i * rowH;

			// Cột 1
			drawCheckboxRow(g, col1X, curY, itemNamesCol1[idx], itemCol1Selected[idx]);

			// Cột 2
			drawCheckboxRow(g, col2X, curY, itemNamesCol2[idx], itemCol2Selected[idx]);
		}

		// Mũi tên cuộn lên / xuống
		if (scrollItemIndex > 0)
		{
			drawTriangleUp(g, boxX + boxW - 11, boxY + headerH + 4);
		}
		if (scrollItemIndex + maxVisibleRows < 12)
		{
			drawTriangleDown(g, boxX + boxW - 11, boxY + headerH + maxVisibleRows * rowH);
		}

		// Ô NHẬP D/S ITEMID TỰ DÙNG (CHO PHÉP NHẬP TÙY Ý)
		int inputEatY = startY + maxVisibleRows * rowH + 4;
		mFont.tahoma_7b_dark.drawString(g, "D/S ItemID Tự Dùng:", boxX + 8, inputEatY + 3, 0);
		int tfEatX = boxX + 122;
		int tfEatW = boxW - 130;
		int tfEatH = 18;
		drawInputField(g, tfEatX, inputEatY, tfEatW, tfEatH, eatItemFilter, "mỗi ItemID cách nhau ','", activeInputIndex == 3);

		// NÚT BẤM DƯỚI CÙNG (HÌNH 2)
		int btnW = 125;
		int btnH = 18;
		int btnY1 = boxY + boxH - 42;
		int btnY2 = boxY + boxH - 22;
		int btnXCenter = boxX + (boxW - btnW) / 2;

		// 1. [ Lưu Cho Tài Khoản ]
		g.setColor(1800936);
		g.fillRect(btnXCenter, btnY1, btnW, btnH);
		g.setColor(2758924);
		g.drawRect(btnXCenter, btnY1, btnW, btnH);
		mFont.tahoma_7b_white.drawString(g, "Lưu Cho Tài Khoản", btnXCenter + btnW / 2, btnY1 + 3, mFont.CENTER);

		// 2. [ Lưu Cho Toàn Bộ ]
		g.setColor(1800936);
		g.fillRect(btnXCenter, btnY2, btnW, btnH);
		g.setColor(2758924);
		g.drawRect(btnXCenter, btnY2, btnW, btnH);
		mFont.tahoma_7b_white.drawString(g, "Lưu Cho Toàn Bộ", btnXCenter + btnW / 2, btnY2 + 3, mFont.CENTER);
	}

	// =============================================================
	// 3. VẼ BẢNG SELECT SKILL (HÌNH 3)
	// =============================================================
	private static void paintSkillDialog(mGraphics g)
	{
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.vSkillFight == null) return;

		int totalSkills = myChar.vSkillFight.size();
		int maxVisible = 7;
		int rowH = 19;
		int headerH = 24;

		int boxW = 165;
		int boxH = headerH + maxVisible * rowH + 6;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;

		// Nền vàng cát
		g.setColor(14860437);
		g.fillRect(boxX, boxY, boxW, boxH);
		g.setColor(2758924);
		g.drawRect(boxX, boxY, boxW, boxH);

		// Tiêu đề xanh: Select Skill
		g.setColor(1800936);
		g.fillRect(boxX, boxY, boxW, headerH);
		mFont.tahoma_7b_white.drawString(g, "Select Skill", boxX + boxW / 2, boxY + 5, mFont.CENTER);

		// Nút [X] đóng
		int btnCloseX = boxX + boxW - 17;
		int btnCloseY = boxY + 4;
		g.setColor(13382451);
		g.fillRect(btnCloseX, btnCloseY, 13, 14);
		mFont.tahoma_7b_white.drawString(g, "X", btnCloseX + 7, btnCloseY + 1, mFont.CENTER);

		// Danh sách các skill
		for (int i = 0; i < maxVisible && (scrollSkillIndex + i) < totalSkills; i++)
		{
			int skillIdx = scrollSkillIndex + i;
			Skill s = (Skill)myChar.vSkillFight.elementAt(skillIdx);
			if (s == null || s.template == null) continue;

			int rowY = boxY + headerH + 4 + i * rowH;
			bool isSelected = selectedSkillIds.Contains(s.template.id);
			drawCheckboxRow(g, boxX + 12, rowY, s.template.name, isSelected);
		}

		// Mũi tên cuộn lên / xuống
		if (scrollSkillIndex > 0)
		{
			drawTriangleUp(g, boxX + boxW - 13, boxY + headerH + 5);
		}
		if (scrollSkillIndex + maxVisible < totalSkills)
		{
			drawTriangleDown(g, boxX + boxW - 13, boxY + boxH - 12);
		}
	}

	// Hàm phụ vẽ ô tròn Checkbox + Chữ
	private static void drawCheckboxRow(mGraphics g, int x, int y, string label, bool isChecked)
	{
		int circleSize = 11;

		g.setColor(3614238);
		g.drawRoundRect(x, y + 2, circleSize, circleSize, 5, 5);

		if (isChecked)
		{
			g.setColor(2627855);
			g.fillRoundRect(x + 2, y + 4, circleSize - 4, circleSize - 4, 3, 3);
		}

		mFont.tahoma_7b_dark.drawString(g, label, x + 15, y + 1, 0);
	}

	private static void drawTriangleUp(mGraphics g, int x, int y)
	{
		g.setColor(1800936);
		for (int i = 0; i < 6; i++) { g.fillRect(x - i, y + i, i * 2 + 1, 1); }
	}

	private static void drawTriangleDown(mGraphics g, int x, int y)
	{
		g.setColor(1800936);
		for (int i = 0; i < 6; i++) { g.fillRect(x - (5 - i), y + i, (5 - i) * 2 + 1, 1); }
	}

	// =============================================================
	// 4. VẼ BẢNG AUTO NHẶT CHUẨN THEO HÌNH ẢNH YÊU CẦU
	// =============================================================
	private static void paintPickDialog(mGraphics g)
	{
		int boxW = 280;
		int boxH = 220;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;
		int headerH = 24;

		// 1. Nền vàng cát sa mạc
		g.setColor(14860437);
		g.fillRect(boxX, boxY, boxW, boxH);
		g.setColor(2758924);
		g.drawRect(boxX, boxY, boxW, boxH);

		// 2. Tiêu đề xanh dương: Auto Nhặt
		g.setColor(2004694);
		g.fillRect(boxX, boxY, boxW, headerH);
		mFont.tahoma_7b_white.drawString(g, "Auto Nhặt", boxX + boxW / 2, boxY + 5, mFont.CENTER);

		// Nút [X] đóng màu đỏ ở góc trên bên phải
		int btnCloseX = boxX + boxW - 18;
		int btnCloseY = boxY + 4;
		g.setColor(13382451);
		g.fillRect(btnCloseX, btnCloseY, 14, 15);
		g.setColor(16777215);
		g.drawRect(btnCloseX, btnCloseY, 14, 15);
		mFont.tahoma_7b_white.drawString(g, "X", btnCloseX + 7, btnCloseY + 2, mFont.CENTER);

		// 3. 4 DÒNG RADIO BUTTONS THEO MẪU ẢNH
		int startY = boxY + headerH + 6;
		int radioRowH = 20;

		drawRadioButton(g, boxX + 12, startY, "Tự Động Nhặt", isAutoPick);
		drawRadioButton(g, boxX + 12, startY + radioRowH, "Nhặt Vật Phẩm Của Người Khác", isPickOthers);
		drawRadioButton(g, boxX + 12, startY + radioRowH * 2, "Nhặt Toàn Map", isPickAllMap);
		drawRadioButton(g, boxX + 12, startY + radioRowH * 3, "Tự Động Vứt Vật Phẩm", isAutoDrop);

		// 4. 3 Ô NHẬP TEXTBOX D/S ITEMID THEO MẪU ẢNH
		int inputStartY = startY + radioRowH * 4 + 6;
		int inputRowH = 24;
		int tfX = boxX + 115;
		int tfW = boxW - 125;
		int tfH = 19;

		// Dòng 1: D/S ItemID Nhặt
		mFont.tahoma_7b_dark.drawString(g, "D/S ItemID Nhặt:", boxX + 12, inputStartY + 3, 0);
		drawInputField(g, tfX, inputStartY, tfW, tfH, pickItemFilter, "cách nhau dấu ','", activeInputIndex == 0);

		// Dòng 2: D/S ItemID Loại Trừ
		mFont.tahoma_7b_dark.drawString(g, "D/S ItemID Loại Trừ:", boxX + 12, inputStartY + inputRowH + 3, 0);
		drawInputField(g, tfX, inputStartY + inputRowH, tfW, tfH, excludeItemFilter, "cách nhau dấu ','", activeInputIndex == 1);

		// Dòng 3: D/S ItemID Vứt Bỏ
		mFont.tahoma_7b_dark.drawString(g, "D/S ItemID Vứt Bỏ:", boxX + 12, inputStartY + inputRowH * 2 + 3, 0);
		drawInputField(g, tfX, inputStartY + inputRowH * 2, tfW, tfH, dropItemFilter, "cách nhau dấu ','", activeInputIndex == 2);

		// 5. Nút [ ĐÓNG BẢNG (ESC) ] dưới đáy
		int btnBottomW = 160;
		int btnBottomH = 22;
		int btnBottomX = boxX + (boxW - btnBottomW) / 2;
		int btnBottomY = boxY + boxH - 26;
		g.setColor(13382451);
		g.fillRect(btnBottomX, btnBottomY, btnBottomW, btnBottomH);
		g.setColor(2758924);
		g.drawRect(btnBottomX, btnBottomY, btnBottomW, btnBottomH);
		mFont.tahoma_7b_white.drawString(g, "ĐÓNG BẢNG (ESC)", btnBottomX + btnBottomW / 2, btnBottomY + 4, mFont.CENTER);
	}

	// Vẽ nút RadioButton tròn
	private static void drawRadioButton(mGraphics g, int x, int y, string label, bool isChecked)
	{
		int size = 13;
		// Vòng tròn viền ngoài nâu sẫm
		g.setColor(4074270);
		g.drawRoundRect(x, y + 2, size, size, 6, 6);
		// Nền trắng kem
		g.setColor(16775130);
		g.fillRoundRect(x + 1, y + 3, size - 2, size - 2, 5, 5);

		// Chấm tròn nâu đậm bên trong nếu được check
		if (isChecked)
		{
			g.setColor(4074270);
			g.fillRoundRect(x + 3, y + 5, size - 6, size - 6, 3, 3);
		}

		mFont.tahoma_7b_dark.drawString(g, label, x + 18, y + 2, 0);
	}

	// Vẽ ô nhập InputField vàng cam viền bo góc, hỗ trợ gõ phím trực tiếp & nhấp nháy con trỏ
	private static void drawInputField(mGraphics g, int x, int y, int w, int h, string content, string placeholder, bool isFocus = false)
	{
		// Nền vàng sáng nếu focus, vàng cam nhạt nếu không focus
		g.setColor(isFocus ? 16776170 : 16107650);
		g.fillRoundRect(x, y, w, h, 6, 6);
		// Viền xanh đậm nếu focus, viền nâu đậm nếu không focus
		g.setColor(isFocus ? 2004694 : 4074270);
		g.drawRoundRect(x, y, w, h, 6, 6);

		g.setClip(x + 3, y + 1, w - 6, h - 2);
		if (string.IsNullOrEmpty(content) && !isFocus)
		{
			mFont.tahoma_7_grey.drawString(g, placeholder, x + 5, y + 3, 0);
		}
		else
		{
			string text = content ?? string.Empty;
			mFont font = isFocus ? mFont.tahoma_7b_dark : mFont.tahoma_7b_red;
			font.drawString(g, text, x + 5, y + 3, 0);

			// Vẽ con trỏ nhấp nháy '|' khi đang focus nhập liệu
			if (isFocus && (mSystem.currentTimeMillis() / 400 % 2 == 0))
			{
				int caretX = x + 5 + font.getWidth(text);
				g.setColor(0);
				g.fillRect(caretX, y + 3, 1, 13);
			}
		}
		GameCanvas.resetTrans(g);
	}

	// =============================================================
	// XỬ LÝ CLICK CHUỘT / TOUCH TRÊN TẤT CẢ CÁC BẢNG POPUP
	// =============================================================
	public static void updateDialogInput()
	{
		// Hỗ trợ phím ESC hoặc M để đóng tất cả các bảng popup ngay lập tức
		try
		{
			if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M))
			{
				closeAllDialogs();
				GameCanvas.isPointerJustRelease = false;
				return;
			}
		}
		catch (Exception)
		{
		}

		// Xử lý gõ phím trực tiếp vào ô Textbox trong Bảng Auto Nhặt / Auto Item
		if ((isShowPickDialog || isShowItemDialog) && activeInputIndex >= 0)
		{
			try
			{
				string inputStr = Input.inputString;
				if (!string.IsNullOrEmpty(inputStr))
				{
					for (int i = 0; i < inputStr.Length; i++)
					{
						char c = inputStr[i];
						if (c == '\b') // Phím Backspace xóa ký tự
						{
							handleInputBackspace();
						}
						else if (c == '\n' || c == '\r') // Phím Enter hoàn tất nhập
						{
							activeInputIndex = -1;
						}
						else if (char.IsDigit(c) || c == ',' || c == ' ' || c == ';')
						{
							handleInputChar(c);
						}
					}
				}

				// Hỗ trợ Paste từ Clipboard (Ctrl + V)
				if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
				{
					string clip = GUIUtility.systemCopyBuffer;
					if (!string.IsNullOrEmpty(clip))
					{
						for (int i = 0; i < clip.Length; i++)
						{
							char c = clip[i];
							if (char.IsDigit(c) || c == ',' || c == ' ' || c == ';')
							{
								handleInputChar(c);
							}
						}
					}
				}
			}
			catch (Exception)
			{
			}
		}

		if (isShowPickDialog)
		{
			handlePickDialogInput();
			return;
		}

		if (isShowSkillDialog)
		{
			handleSkillDialogInput();
			return;
		}

		if (isShowItemDialog)
		{
			handleItemDialogInput();
			return;
		}

		if (isShowMainDialog)
		{
			handleMainDialogInput();
			return;
		}
	}

	private static void handlePickDialogInput()
	{
		int boxW = 280;
		int boxH = 220;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;
		int headerH = 24;

		long now = mSystem.currentTimeMillis();
		bool isRelease = GameCanvas.isPointerJustRelease || Input.GetMouseButtonUp(0);
		if (isRelease)
		{
			if (now - lastDialogClickTime < 200L)
			{
				GameCanvas.isPointerJustRelease = false;
				return;
			}
			lastDialogClickTime = now;

			int px = GameCanvas.px;
			int py = GameCanvas.py;

			// 1. Nút [X] đóng bảng
			if (px >= boxX + boxW - 24 && px <= boxX + boxW && py >= boxY && py <= boxY + headerH)
			{
				isShowPickDialog = false;
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// 2. Click 4 dòng Radio Buttons
			int startY = boxY + headerH + 6;
			int radioRowH = 20;

			if (px >= boxX + 10 && px <= boxX + boxW - 10)
			{
				// Dòng 1: Tự Động Nhặt
				if (py >= startY && py < startY + radioRowH)
				{
					activeInputIndex = -1;
					isAutoPick = !isAutoPick;
					notify("Tự Động Nhặt: " + (isAutoPick ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Dòng 2: Nhặt Vật Phẩm Của Người Khác
				else if (py >= startY + radioRowH && py < startY + radioRowH * 2)
				{
					activeInputIndex = -1;
					isPickOthers = !isPickOthers;
					notify("Nhặt của người khác: " + (isPickOthers ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Dòng 3: Nhặt Toàn Map
				else if (py >= startY + radioRowH * 2 && py < startY + radioRowH * 3)
				{
					activeInputIndex = -1;
					isPickAllMap = !isPickAllMap;
					notify("Nhặt Toàn Map: " + (isPickAllMap ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Dòng 4: Tự Động Vứt Vật Phẩm
				else if (py >= startY + radioRowH * 3 && py < startY + radioRowH * 4)
				{
					activeInputIndex = -1;
					isAutoDrop = !isAutoDrop;
					notify("Tự Động Vứt: " + (isAutoDrop ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}

			// 3. Click 3 ô Textbox nhập ItemID - CHỌN FOCUS ĐỂ GÕ TRỰC TIẾP TRÊN BẢNG, KHÔNG ĐÓNG BẢNG
			int inputStartY = startY + radioRowH * 4 + 6;
			int inputRowH = 24;

			if (px >= boxX + 10 && px <= boxX + boxW - 10)
			{
				// Ô 1: D/S ItemID Nhặt
				if (py >= inputStartY && py < inputStartY + inputRowH)
				{
					activeInputIndex = 0;
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Ô 2: D/S ItemID Loại Trừ
				else if (py >= inputStartY + inputRowH && py < inputStartY + inputRowH * 2)
				{
					activeInputIndex = 1;
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Ô 3: D/S ItemID Vứt Bỏ
				else if (py >= inputStartY + inputRowH * 2 && py < inputStartY + inputRowH * 3)
				{
					activeInputIndex = 2;
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}

			// 4. Nút [ ĐÓNG BẢNG (ESC) ] dưới đáy
			int btnBottomW = 160;
			int btnBottomH = 22;
			int btnBottomX = boxX + (boxW - btnBottomW) / 2;
			int btnBottomY = boxY + boxH - 26;
			if (px >= btnBottomX - 6 && px <= btnBottomX + btnBottomW + 6 && py >= btnBottomY - 4 && py <= btnBottomY + btnBottomH + 6)
			{
				closeAllDialogs();
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// 5. Click ra ngoài bảng -> Đóng bảng
			if (px < boxX - 10 || px > boxX + boxW + 10 || py < boxY - 10 || py > boxY + boxH + 10)
			{
				closeAllDialogs();
			}

			GameCanvas.isPointerJustRelease = false;
		}
	}

	private static void handleMainDialogInput()
	{
		int boxW = 200;
		int boxH = 295;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;
		int headerH = 24;

		long now = mSystem.currentTimeMillis();
		bool isRelease = GameCanvas.isPointerJustRelease || Input.GetMouseButtonUp(0);
		if (isRelease)
		{
			if (now - lastDialogClickTime < 200L)
			{
				GameCanvas.isPointerJustRelease = false;
				return;
			}
			lastDialogClickTime = now;

			int px = GameCanvas.px;
			int py = GameCanvas.py;

			// 1. Nút [X] đóng bảng ở góc trên bên phải
			if (px >= boxX + boxW - 24 && px <= boxX + boxW && py >= boxY && py <= boxY + headerH)
			{
				isShowMainDialog = false;
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// 2. Tích chọn 7 tính năng chính
			int startY = boxY + headerH + 6;
			int rowH = 18;

			if (px >= boxX + 6 && px <= boxX + boxW - 6)
			{
				if (py >= startY && py < startY + rowH) { isTanSat = !isTanSat; notify("Tàn sát: " + (isTanSat ? "BẬT" : "TẮT")); GameCanvas.isPointerJustRelease = false; return; }
				else if (py >= startY + rowH && py < startY + rowH * 2) { isAutoTrain = !isAutoTrain; notify("Tự luyện tập: " + (isAutoTrain ? "BẬT" : "TẮT")); GameCanvas.isPointerJustRelease = false; return; }
				else if (py >= startY + rowH * 2 && py < startY + rowH * 3) { isAutoChangeZone = !isAutoChangeZone; notify("Đổi khu khi hết quái: " + (isAutoChangeZone ? "BẬT" : "TẮT")); GameCanvas.isPointerJustRelease = false; return; }
				else if (py >= startY + rowH * 3 && py < startY + rowH * 4) { isAutoPick = !isAutoPick; notify("Tự động nhặt: " + (isAutoPick ? "BẬT" : "TẮT")); GameCanvas.isPointerJustRelease = false; return; }
				else if (py >= startY + rowH * 4 && py < startY + rowH * 5) { isBuaThuHut = !isBuaThuHut; notify("Bùa thu hút: " + (isBuaThuHut ? "BẬT" : "TẮT")); GameCanvas.isPointerJustRelease = false; return; }
				else if (py >= startY + rowH * 5 && py < startY + rowH * 6) { isAutoItem = !isAutoItem; notify("Auto Item: " + (isAutoItem ? "BẬT" : "TẮT")); GameCanvas.isPointerJustRelease = false; return; }
				else if (py >= startY + rowH * 6 && py < startY + rowH * 7) { isShowHUD = !isShowHUD; notify("Bảng HUD: " + (isShowHUD ? "BẬT" : "TẮT")); GameCanvas.isPointerJustRelease = false; return; }
			}

			// 3. Nút mở 3 bảng phụ: [ Skill ] [ Item ] [ Nhặt ]
			int btnSubY = startY + rowH * 7 + 4;
			if (py >= btnSubY && py <= btnSubY + 20)
			{
				// Nút Skill
				if (px >= boxX + 10 && px <= boxX + 67)
				{
					isShowMainDialog = false;
					showSelectSkillMenu();
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Nút Item
				if (px >= boxX + 71 && px <= boxX + 128)
				{
					isShowMainDialog = false;
					showAutoItemMenu();
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Nút Nhặt
				if (px >= boxX + 132 && px <= boxX + 190)
				{
					isShowMainDialog = false;
					showAutoPickMenu();
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}

			// 4. Các nút tốc độ game & di chuyển (1x -> 10x và nút [-] / [+])
			int speedY = btnSubY + 23;
			int btnMiniW = 20;
			int btnMiniH = 14;
			int btnMinusX = boxX + 140;
			int btnPlusX = boxX + 168;

			// Nút [-]
			if (px >= btnMinusX && px <= btnMinusX + btnMiniW && py >= speedY - 1 && py <= speedY - 1 + btnMiniH)
			{
				setSpeed(Mathf.Max(1f, currentSpeed - 1f));
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Nút [+]
			if (px >= btnPlusX && px <= btnPlusX + btnMiniW && py >= speedY - 1 && py <= speedY - 1 + btnMiniH)
			{
				setSpeed(Mathf.Min(10f, currentSpeed + 1f));
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			float[] speeds = new float[] { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f };
			int btnSpdW = 32;
			int btnSpdH = 15;
			int gap = 5;
			int startBtnSpdX = boxX + 10;
			int btnSpdY1 = speedY + 14;
			int btnSpdY2 = speedY + 31;

			// Hàng 1: 1x -> 5x
			if (py >= btnSpdY1 && py <= btnSpdY1 + btnSpdH)
			{
				for (int i = 0; i < 5; i++)
				{
					int curBtnX = startBtnSpdX + i * (btnSpdW + gap);
					if (px >= curBtnX && px <= curBtnX + btnSpdW)
					{
						setSpeed(speeds[i]);
						GameCanvas.isPointerJustRelease = false;
						return;
					}
				}
			}

			// Hàng 2: 6x -> 10x
			if (py >= btnSpdY2 && py <= btnSpdY2 + btnSpdH)
			{
				for (int j = 5; j < 10; j++)
				{
					int curBtnX = startBtnSpdX + (j - 5) * (btnSpdW + gap);
					if (px >= curBtnX && px <= curBtnX + btnSpdW)
					{
						setSpeed(speeds[j]);
						GameCanvas.isPointerJustRelease = false;
						return;
					}
				}
			}

			// 5. Nút [ ĐÓNG BẢNG (ESC) ] dưới đáy bảng
			int btnCloseBottomW = 140;
			int btnCloseBottomH = 18;
			int btnCloseBottomX = boxX + (boxW - btnCloseBottomW) / 2;
			int btnCloseBottomY = btnSpdY2 + 20;
			if (px >= btnCloseBottomX && px <= btnCloseBottomX + btnCloseBottomW && py >= btnCloseBottomY && py <= btnCloseBottomY + btnCloseBottomH)
			{
				isShowMainDialog = false;
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// 6. Click ra ngoài bảng -> Đóng bảng
			if (px < boxX || px > boxX + boxW || py < boxY || py > boxY + boxH)
			{
				isShowMainDialog = false;
			}

			GameCanvas.isPointerJustRelease = false;
		}
	}

	private static void handleItemDialogInput()
	{
		int boxW = 275;
		int boxH = 245;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;
		int headerH = 24;

		// Cuộn chuột
		try
		{
			float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
			if (scrollWheel < 0f && scrollItemIndex + 8 < 12)
			{
				scrollItemIndex++;
			}
			else if (scrollWheel > 0f && scrollItemIndex > 0)
			{
				scrollItemIndex--;
			}
		}
		catch (Exception)
		{
		}

		long now = mSystem.currentTimeMillis();
		bool isRelease = GameCanvas.isPointerJustRelease || Input.GetMouseButtonUp(0);
		if (isRelease)
		{
			if (now - lastDialogClickTime < 200L)
			{
				GameCanvas.isPointerJustRelease = false;
				return;
			}
			lastDialogClickTime = now;

			int px = GameCanvas.px;
			int py = GameCanvas.py;

			// Nút [X] đóng bảng
			if (px >= boxX + boxW - 20 && px <= boxX + boxW && py >= boxY && py <= boxY + headerH)
			{
				activeInputIndex = -1;
				if (hasAnyItemSelected()) isAutoItem = true;
				saveSettings();
				isShowItemDialog = false;
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Mũi tên cuộn
			if (scrollItemIndex > 0 && px >= boxX + boxW - 18 && px <= boxX + boxW && py >= boxY + headerH && py <= boxY + headerH + 18)
			{
				scrollItemIndex--;
				GameCanvas.isPointerJustRelease = false;
				return;
			}
			if (scrollItemIndex + 8 < 12 && px >= boxX + boxW - 18 && px <= boxX + boxW && py >= boxY + headerH + 8 * 17 - 18 && py <= boxY + headerH + 8 * 17)
			{
				scrollItemIndex++;
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Tích chọn trong 2 Cột
			int maxVisibleRows = 8;
			int rowH = 17;
			int startY = boxY + headerH + 5;

			int col1X = boxX + 8;
			int col2X = boxX + 138;

			for (int i = 0; i < maxVisibleRows && (scrollItemIndex + i) < 12; i++)
			{
				int idx = scrollItemIndex + i;
				int curY = startY + i * rowH;

				// Click Cột 1
				if (px >= col1X && px < col1X + 125 && py >= curY && py < curY + rowH)
				{
					itemCol1Selected[idx] = !itemCol1Selected[idx];
					if (itemCol1Selected[idx]) isAutoItem = true;
					notify(itemNamesCol1[idx] + ": " + (itemCol1Selected[idx] ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}

				// Click Cột 2
				if (px >= col2X && px < col2X + 125 && py >= curY && py < curY + rowH)
				{
					itemCol2Selected[idx] = !itemCol2Selected[idx];
					if (itemCol2Selected[idx]) isAutoItem = true;
					notify(itemNamesCol2[idx] + ": " + (itemCol2Selected[idx] ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}

			// Click ô nhập D/S ItemID Tự Dùng
			int inputEatY = startY + maxVisibleRows * rowH + 4;
			int tfEatX = boxX + 122;
			int tfEatW = boxW - 130;
			int tfEatH = 18;

			if (px >= boxX + 10 && px <= boxX + boxW - 10 && py >= inputEatY - 3 && py <= inputEatY + tfEatH + 3)
			{
				activeInputIndex = 3;
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Nút [ Lưu Cho Tài Khoản ] hoặc [ Lưu Cho Toàn Bộ ]
			int btnY1 = boxY + boxH - 42;
			int btnY2 = boxY + boxH - 22;
			int btnW = 125;
			int btnXCenter = boxX + (boxW - btnW) / 2;

			if (px >= btnXCenter && px <= btnXCenter + btnW)
			{
				if ((py >= btnY1 && py <= btnY1 + 18) || (py >= btnY2 && py <= btnY2 + 18))
				{
					activeInputIndex = -1;
					isAutoItem = true;
					saveSettings();
					isShowItemDialog = false;
					notify("Đã lưu & BẬT Auto Item thành công!");
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}

			// Click ra ngoài đóng bảng
			if (px < boxX || px > boxX + boxW || py < boxY || py > boxY + boxH)
			{
				activeInputIndex = -1;
				if (hasAnyItemSelected()) isAutoItem = true;
				saveSettings();
				isShowItemDialog = false;
			}

			GameCanvas.isPointerJustRelease = false;
		}
	}

	private static void handleSkillDialogInput()
	{
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.vSkillFight == null)
		{
			isShowSkillDialog = false;
			return;
		}

		int totalSkills = myChar.vSkillFight.size();
		int maxVisible = 7;
		int rowH = 19;
		int headerH = 24;

		int boxW = 165;
		int boxH = headerH + maxVisible * rowH + 6;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;

		// Hỗ trợ cuộn chuột
		try
		{
			float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
			if (scrollWheel < 0f && scrollSkillIndex + maxVisible < totalSkills)
			{
				scrollSkillIndex++;
			}
			else if (scrollWheel > 0f && scrollSkillIndex > 0)
			{
				scrollSkillIndex--;
			}
		}
		catch (Exception)
		{
		}

		long now = mSystem.currentTimeMillis();
		bool isRelease = GameCanvas.isPointerJustRelease || Input.GetMouseButtonUp(0);
		if (isRelease)
		{
			if (now - lastDialogClickTime < 200L)
			{
				GameCanvas.isPointerJustRelease = false;
				return;
			}
			lastDialogClickTime = now;

			int px = GameCanvas.px;
			int py = GameCanvas.py;

			// Nút [X] đóng bảng
			if (px >= boxX + boxW - 20 && px <= boxX + boxW && py >= boxY && py <= boxY + headerH)
			{
				isShowSkillDialog = false;
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Mũi tên lên / xuống
			if (scrollSkillIndex > 0 && px >= boxX + boxW - 20 && px <= boxX + boxW && py >= boxY + headerH && py <= boxY + headerH + 20)
			{
				scrollSkillIndex--;
				GameCanvas.isPointerJustRelease = false;
				return;
			}
			if (scrollSkillIndex + maxVisible < totalSkills && px >= boxX + boxW - 20 && px <= boxX + boxW && py >= boxY + boxH - 20 && py <= boxY + boxH)
			{
				scrollSkillIndex++;
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Click vào dòng kỹ năng để tích chọn
			for (int i = 0; i < maxVisible && (scrollSkillIndex + i) < totalSkills; i++)
			{
				int rowY = boxY + headerH + 4 + i * rowH;
				if (px >= boxX + 6 && px <= boxX + boxW - 16 && py >= rowY && py <= rowY + rowH)
				{
					int skillIdx = scrollSkillIndex + i;
					Skill s = (Skill)myChar.vSkillFight.elementAt(skillIdx);
					if (s != null && s.template != null)
					{
						int sId = s.template.id;
						if (selectedSkillIds.Contains(sId))
						{
							selectedSkillIds.Remove(sId);
							notify("Bỏ chọn skill: " + s.template.name);
						}
						else
						{
							selectedSkillIds.Add(sId);
							notify("Đã tích skill: " + s.template.name);
						}
					}
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}

			if (px < boxX || px > boxX + boxW || py < boxY || py > boxY + boxH)
			{
				isShowSkillDialog = false;
			}

			GameCanvas.isPointerJustRelease = false;
		}
	}

	// Kiểm tra click vào nút [⚙ Menu (F2)]
	public static bool checkHUDButtonClick()
	{
		if (isShowMainDialog || isShowSkillDialog || isShowItemDialog || isShowPickDialog)
		{
			return false;
		}

		if (GameCanvas.isPointerJustRelease)
		{
			int btnX = 6;
			int btnY = 45;
			int btnW = 85;
			int btnH = 16;

			if (GameCanvas.px >= btnX && GameCanvas.px <= btnX + btnW && GameCanvas.py >= btnY && GameCanvas.py <= btnY + btnH)
			{
				showMainControlDialog();
				GameCanvas.isPointerJustRelease = false;
				return true;
			}
		}
		return false;
	}

	// -------------------------------------------------------------
	// CÁC HÀM MỞ BẢNG POPUP
	// -------------------------------------------------------------
	public static void showMainControlDialog()
	{
		isShowSkillDialog = false;
		isShowItemDialog = false;
		isShowPickDialog = false;
		isShowMainDialog = true;
	}

	public static void showSelectSkillMenu()
	{
		isShowMainDialog = false;
		isShowItemDialog = false;
		isShowPickDialog = false;
		scrollSkillIndex = 0;
		isShowSkillDialog = true;
	}

	public static void showAutoItemMenu()
	{
		isShowMainDialog = false;
		isShowSkillDialog = false;
		isShowPickDialog = false;
		scrollItemIndex = 0;
		isShowItemDialog = true;
	}

	public static void showAutoPickMenu()
	{
		isShowMainDialog = false;
		isShowSkillDialog = false;
		isShowItemDialog = false;
		isShowPickDialog = true;
	}

	public void showMainMenu()
	{
		showMainControlDialog();
	}

	public void perform(int idAction, object p)
	{
		if (idAction == 10001) // Lưu D/S ItemID Nhặt
		{
			if (GameCanvas.inputDlg != null && GameCanvas.inputDlg.tfInput != null)
			{
				pickItemFilter = GameCanvas.inputDlg.tfInput.getText();
			}
			GameCanvas.endDlg();
			showAutoPickMenu();
			notify("Đã lưu D/S ItemID Nhặt: " + pickItemFilter);
		}
		else if (idAction == 10002) // Lưu D/S ItemID Loại Trừ
		{
			if (GameCanvas.inputDlg != null && GameCanvas.inputDlg.tfInput != null)
			{
				excludeItemFilter = GameCanvas.inputDlg.tfInput.getText();
			}
			GameCanvas.endDlg();
			showAutoPickMenu();
			notify("Đã lưu D/S ItemID Loại Trừ: " + excludeItemFilter);
		}
		else if (idAction == 10003) // Lưu D/S ItemID Vứt Bỏ
		{
			if (GameCanvas.inputDlg != null && GameCanvas.inputDlg.tfInput != null)
			{
				dropItemFilter = GameCanvas.inputDlg.tfInput.getText();
			}
			GameCanvas.endDlg();
			showAutoPickMenu();
			notify("Đã lưu D/S ItemID Vứt Bỏ: " + dropItemFilter);
		}
		else if (idAction == 10004) // Lưu D/S ItemID Tự Dùng / Ăn
		{
			if (GameCanvas.inputDlg != null && GameCanvas.inputDlg.tfInput != null)
			{
				eatItemFilter = GameCanvas.inputDlg.tfInput.getText();
			}
			saveSettings();
			GameCanvas.endDlg();
			showAutoItemMenu();
			notify("Đã lưu D/S ItemID Tự Dùng: " + eatItemFilter);
		}
	}

	public static bool hasAnyItemSelected()
	{
		for (int i = 0; i < itemCol1Selected.Length; i++)
		{
			if (itemCol1Selected[i]) return true;
		}
		for (int i = 0; i < itemCol2Selected.Length; i++)
		{
			if (itemCol2Selected[i]) return true;
		}
		if (!string.IsNullOrEmpty(eatItemFilter)) return true;
		return false;
	}

	public static void saveSettings()
	{
		try
		{
			Rms.saveRMSString("mod_is_auto_item", isAutoItem ? "1" : "0");
			Rms.saveRMSString("mod_eat_filter", eatItemFilter ?? "");
			Rms.saveRMSString("mod_drop_filter", dropItemFilter ?? "");
			Rms.saveRMSString("mod_pick_filter", pickItemFilter ?? "");
			Rms.saveRMSString("mod_exclude_filter", excludeItemFilter ?? "");

			string col1Str = "";
			for (int i = 0; i < itemCol1Selected.Length; i++) col1Str += (itemCol1Selected[i] ? "1" : "0");
			Rms.saveRMSString("mod_item_col1", col1Str);

			string col2Str = "";
			for (int i = 0; i < itemCol2Selected.Length; i++) col2Str += (itemCol2Selected[i] ? "1" : "0");
			Rms.saveRMSString("mod_item_col2", col2Str);
		}
		catch (Exception)
		{
		}
	}

	public static void loadSettings()
	{
		try
		{
			string autoItem = Rms.loadRMSString("mod_is_auto_item");
			if (autoItem != null)
			{
				isAutoItem = (autoItem == "1");
			}

			string eat = Rms.loadRMSString("mod_eat_filter");
			if (eat != null) eatItemFilter = eat;

			string drop = Rms.loadRMSString("mod_drop_filter");
			if (drop != null) dropItemFilter = drop;

			string pick = Rms.loadRMSString("mod_pick_filter");
			if (pick != null) pickItemFilter = pick;

			string exc = Rms.loadRMSString("mod_exclude_filter");
			if (exc != null) excludeItemFilter = exc;

			string col1 = Rms.loadRMSString("mod_item_col1");
			if (!string.IsNullOrEmpty(col1))
			{
				for (int i = 0; i < itemCol1Selected.Length && i < col1.Length; i++)
					itemCol1Selected[i] = (col1[i] == '1');
			}

			string col2 = Rms.loadRMSString("mod_item_col2");
			if (!string.IsNullOrEmpty(col2))
			{
				for (int i = 0; i < itemCol2Selected.Length && i < col2.Length; i++)
					itemCol2Selected[i] = (col2[i] == '1');
			}
		}
		catch (Exception)
		{
		}
	}
}
