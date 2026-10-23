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
	public static int activeMainDialogTab = 0;    // 0: Chiến Đấu, 1: Săn Boss, 2: Trong Khu, 3: Tự Nhặt, 4: Auto Item, 5: Đồ Hoạ, 6: FPS, 7: Hệ Thống
	public static bool isShowSkillDialog = false; // Bảng Select Skill (Hình 3)
	public static bool isShowItemDialog = false;  // Bảng Auto Item 2 cột (Hình 1 & 2)
	public static bool isShowPickDialog = false;  // Bảng Auto Nhặt (Hình mẫu người dùng)
	public static bool isShowMobDialog = false;   // Bảng Chọn Quái Theo Vị Trí (Hình mẫu người dùng)
	public static bool isShowGobackDialog = false; // Bảng Cài Đặt Goback Riêng Biệt (Tách riêng theo yêu cầu)

	// Đứng im 1 chỗ pem quái & Lọc quái chỉ định (Auto Train / Tàn Sát vị trí)
	public static bool isStandStill = false;          // Đứng im 1 chỗ pem quái toàn map (Không bao giờ dịch chuyển tới quái)
	public static bool isFilterMob = false;           // Chỉ pem quái theo vị trí chỉ định
	public static List<int> selectedMobIds = new List<int>(); // Danh sách mobId cụ thể được chọn trong map
	public static int scrollMobIndex = 0;             // Cuộn danh sách quái

	// Auto Hợp Thể / Tách Bông tai siêu tốc
	public static bool isPendingHopThe = false;
	public static long pendingHopTheTime = 0L;
	public static bool isAutoRetryHopThe = false;
	public static long autoRetryHopTheUntil = 0L;
	public static long lastHopTheRequestTime = 0L;
	public static bool initialFusedStateWhenPressedF = false;

	public static void onServerCooldownMessage(string alert)
	{
		lastHopTheRequestTime = mSystem.currentTimeMillis();
		autoOpenWaitUntil = mSystem.currentTimeMillis() + 1500L;
	}

	// Auto Open Item
	public static bool isAutoOpeningItem = false;
	public static Item autoOpenPendingItem = null;
	public static int autoOpenTemplateId = -1;
	public static string autoOpenItemName = "";
	public static int autoOpenMode = 0; // 0: Theo số lượng, 1: Theo mili giây
	public static int autoOpenTargetCount = -1;
	public static int autoOpenRemainingCount = 0;
	public static int autoOpenCountOpened = 0;
	public static long autoOpenIntervalMs = 200L;
	public static long lastAutoOpenTime = 0L;
	public static long autoOpenWaitUntil = 0L;
	private static long lastAutoOpenBagFullCheckTime = 0L;

	// Quản Lý Tài Khoản (QLTK) & Auto Login
	public static bool isCustomResolutionSet = false;
	public static bool isAutoLoginFromQLTK = false;
	public static string autoLoginUser = "";
	public static string autoLoginPass = "";
	public static int autoLoginServerIndex = 0;
	public static int autoLoginPendingFrames = -1;
	public static int autoLoginState = 0; // 0: Init, 1: Connecting, 2: Connected, ready to login
	public static long autoLoginTimer = 0L;

	public static bool isAnyDialogOpen
	{
		get
		{
			return isShowMainDialog || isShowSkillDialog || isShowItemDialog || isShowPickDialog || isShowMobDialog || isShowGobackDialog || XmapManager.isShowXmapDialog;
		}
	}

	public static bool isWaitingDropConfirm = false;
	public static bool isAutoDropAction = false;
	public static bool isManualDropAction = false;
	public static bool isSecurityCodeLocked = false;
	public static int autoDropBagIndex = -1;
	public static int autoDropTemplateId = -1;
	public static int activeInputIndex = -1; // -1: không focus, 0: Nhặt, 1: Loại trừ, 2: Vứt bỏ
	private static int lastInputProcessedFrame = -1;
	public static long pauseAttackUntil = 0L;
	public static long lastHotkeyActionTime = 0L;
	private static Dictionary<KeyCode, int> lastKeyHandledFrame = new Dictionary<KeyCode, int>();
	private static Dictionary<KeyCode, long> lastKeyHandledTime = new Dictionary<KeyCode, long>();

	public static long dropRateLimitUntil = 0L;
	public static ItemObject pendingDropItemObject = null;
	public static long pendingDropConfirmTime = 0L;
	public static long securityCodeLockedUntil = 0L;
	public static int currentDropBagIdx = -1;
	public static int currentDropTemplateId = -1;
	public static Dictionary<int, int> dropFailCounts = new Dictionary<int, int>();
	public static Dictionary<int, long> dropSkipUntil = new Dictionary<int, long>();

	public static void recordDropFail(int templateId)
	{
		if (templateId < 0) return;
		if (!dropFailCounts.ContainsKey(templateId))
		{
			dropFailCounts[templateId] = 0;
		}
		dropFailCounts[templateId]++;
		if (dropFailCounts[templateId] >= 3)
		{
			// Đã thử 3 lần nhưng server từ chối vứt (ví dụ ngọc rồng, đồ khóa, đồ không thể vứt...)
			// Bỏ qua ID này 30s để nhường chỗ vứt các món khác tiếp theo trong danh sách
			dropSkipUntil[templateId] = mSystem.currentTimeMillis() + 30000L;
			dropFailCounts[templateId] = 0;
		}
	}

	public static void onDropSuccess(int bagIndex)
	{
		isWaitingDropConfirm = false;
		isAutoDropAction = false;
		pendingDropItemObject = null;
		lastDropItemTime = mSystem.currentTimeMillis();
		if (currentDropTemplateId != -1)
		{
			dropFailCounts.Remove(currentDropTemplateId);
			dropSkipUntil.Remove(currentDropTemplateId);
		}
	}

	public static void onDropRateLimited()
	{
		long now = mSystem.currentTimeMillis();
		// Server báo "Từ từ đừng vội như vậy...": Hoãn 2.5s (2500ms) theo đúng yêu cầu người dùng
		dropRateLimitUntil = now + 2500L;
		lastDropItemTime = now;
		if (pendingDropItemObject != null)
		{
			// Nếu đang có yêu cầu vứt chưa xác nhận, lùi thời gian xác nhận lại sau 2.5s
			pendingDropConfirmTime = now + 2500L;
		}
		else
		{
			// Nếu lệnh vừa gửi bị từ chối do tốc độ, đợi 2.5s rồi gửi lại
			isWaitingDropConfirm = false;
			isAutoDropAction = false;
			if (currentDropTemplateId != -1)
			{
				recordDropFail(currentDropTemplateId);
			}
		}
	}

	public static void onSecurityCodeUnlocked()
	{
		isSecurityCodeLocked = false;
		securityCodeLockedUntil = 0L;
		isWaitingDropConfirm = false;
		isAutoDropAction = false;
		lastDropItemTime = mSystem.currentTimeMillis() + 1000L;
	}

	public static void closeAllDialogs()
	{
		isShowMainDialog = false;
		isShowSkillDialog = false;
		isShowItemDialog = false;
		isShowPickDialog = false;
		isShowMobDialog = false;
		isShowGobackDialog = false;
		XmapManager.isShowXmapDialog = false;
		activeInputIndex = -1;
		lastDialogClickTime = mSystem.currentTimeMillis();
		GameCanvas.isPointerJustRelease = false;
		BossManager.lastClickedTarget = null;
		BossManager.ClearHitBoxes();
		saveSettings();
	}

	public static bool isItemInDropList(int templateId)
	{
		List<int> dropIds = parseFilterIds(dropItemFilter);
		return dropIds.Contains(templateId);
	}

	private static void handleInputBackspace()
	{
		if (activeInputIndex >= 0 && activeInputIndex < inputFieldScrollOffsets.Length)
		{
			inputFieldScrollOffsets[activeInputIndex] = int.MaxValue;
		}

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
			clearDropBlacklist();
		}
		else if (activeInputIndex == 3)
		{
			if (!string.IsNullOrEmpty(eatItemFilter))
				eatItemFilter = eatItemFilter.Substring(0, eatItemFilter.Length - 1);
		}
		else if (activeInputIndex == 4)
		{
			if (!string.IsNullOrEmpty(gobackPosText))
				gobackPosText = gobackPosText.Substring(0, gobackPosText.Length - 1);
			applyGobackPosFromText();
		}
	}

	private static void handleInputChar(char c)
	{
		if (activeInputIndex >= 0 && activeInputIndex < inputFieldScrollOffsets.Length)
		{
			inputFieldScrollOffsets[activeInputIndex] = int.MaxValue;
		}

		if (activeInputIndex == 0)
		{
			if (pickItemFilter == null) pickItemFilter = string.Empty;
			if (pickItemFilter.Length < 500) pickItemFilter += c;
		}
		else if (activeInputIndex == 1)
		{
			if (excludeItemFilter == null) excludeItemFilter = string.Empty;
			if (excludeItemFilter.Length < 500) excludeItemFilter += c;
		}
		else if (activeInputIndex == 2)
		{
			if (dropItemFilter == null) dropItemFilter = string.Empty;
			if (dropItemFilter.Length < 500) dropItemFilter += c;
			clearDropBlacklist();
		}
		else if (activeInputIndex == 3)
		{
			if (eatItemFilter == null) eatItemFilter = string.Empty;
			if (eatItemFilter.Length < 500) eatItemFilter += c;
		}
		else if (activeInputIndex == 4)
		{
			if (gobackPosText == null) gobackPosText = string.Empty;
			if (gobackPosText.Length < 60) gobackPosText += c;
			applyGobackPosFromText();
		}
	}

	public static void applyGobackPosFromText()
	{
		if (XmapManager.parseGobackPosText(gobackPosText, out int mId, out int zId, out int x, out int y))
		{
			XmapManager.gobackMapId = mId;
			XmapManager.gobackZoneId = zId;
			XmapManager.gobackX = x;
			XmapManager.gobackY = y;
			XmapManager.isManualTravel = false;
		}
	}

	// Cài đặt chức năng Auto Nhặt & Tự vứt rác (Mặc định khi vào game TẤT CẢ TẮT, chỉ bật khi người dùng tự click)
	public static bool isPickOthers = false;      // Nhặt vật phẩm của người khác (Mặc định TẮT)
	public static bool isPickAllMap = false;      // Nhặt toàn map (Mặc định TẮT)
	public static bool isAutoDrop = false;        // Tự động vứt vật phẩm (Mặc định TẮT)

	public static string pickItemFilter = "";
	public static string excludeItemFilter = "";
	public static string dropItemFilter = "";
	public static string eatItemFilter = "";
	public static string gobackPosText = "";

	public static HashSet<int> undroppableItemIds = new HashSet<int>();
	public static Dictionary<int, int> dropAttemptCounts = new Dictionary<int, int>();

	public static void clearDropBlacklist()
	{
		undroppableItemIds.Clear();
		dropAttemptCounts.Clear();
		dropFailCounts.Clear();
		dropSkipUntil.Clear();
		dropRateLimitUntil = 0L;
		pendingDropItemObject = null;
		isWaitingDropConfirm = false;
		isAutoDropAction = false;
	}

	public static long lastDropItemTime = 0L;
	private static int currentDropCandidateIndex = 0;
	public static long lastDialogClickTime = 0L;
	private static Dictionary<int, long> lastTimeUsedCustomItems = new Dictionary<int, long>();

	public static bool isZoneChangeMessage(string text)
	{
		if (string.IsNullOrEmpty(text)) return false;
		string s = removeAccents(text).ToLower();
		if (s.Contains("chuyen khu") || s.Contains("khu vuc")) return true;
		if (s.Contains("vui long cho") || s.Contains("vui long doi")) return true;
		if (s.Contains("chua the chuyen") || s.Contains("khong the chuyen")) return true;
		if (s.Contains("cho") && (s.Contains("giay") || s.Contains("s nua") || s.Contains("s "))) return true;
		if (s.Contains("khong the den") || s.Contains("khong the vao")) return true;
		if (s.Contains("khu day") || s.Contains("khu qua tai") || s.Contains("qua tai") || s.Contains("da day"))
		{
			if (s.Contains("khu") || s.Contains("map") || s.Contains("vuc")) return true;
		}
		if (s.Contains("chua the") && (s.Contains("khu") || s.Contains("luc nay"))) return true;
		return false;
	}

	public static void showBirdMessage(string text)
	{
		try
		{
			if (GameScr.info1 != null)
			{
				GameScr.info1.isUpdate = true;
				GameScr.info1.addInfo(text, 0);
			}
			Char.isLockKey = false;
			ChatPopup.currChatPopup = null;
			ChatPopup.serverChatPopUp = null;
			GameCanvas.endDlg();
			InfoDlg.hide();
			onZoneChangeBlocked();
		}
		catch (Exception)
		{
		}
	}

	public static void onZoneChangeBlocked()
	{
		isWaitingZoneData = false;
		isSilentZoneRequest = false;
		lastZoneChangeTime = mSystem.currentTimeMillis();
		lastZoneAttemptTime = mSystem.currentTimeMillis();
		Char.isLockKey = false;
		ChatPopup.currChatPopup = null;
		ChatPopup.serverChatPopUp = null;
		GameCanvas.endDlg();
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
	// ĐIỀU CHỈNH TỐC ĐỘ XỬ LÝ KHUNG HÌNH (FRAME PACING & SPEED)
	// =============================================================
	public static float currentSpeed = 3f;

	// =============================================================
	// CÀI ĐẶT SKILL DÙNG KHI TÀN SÁT & TỰ LUYỆN TẬP
	// =============================================================
	public static List<int> selectedSkillIds = new List<int>();
	public static int scrollSkillIndex = 0;

	// Quản lý chuyển khu & Auto Vào Khu
	public static bool isWaitingZoneData = false;
	public static bool isSilentZoneRequest = false;
	public static bool isAutoEnterZone = false;
	public static int autoEnterTargetZone = -1;
	public static int autoEnterTargetMap = -1;
	private static long lastAutoEnterZoneAttempt = 0L;
	private static long lastAutoBuffCheckTime = 0L;
	private static long lastTanSatUpdateTime = 0L;
	private static long lastAutoChangeZoneCheckTime = 0L;
	private static long lastAutoItemCheckTime = 0L;
	private static long lastAutoPickCheckTime = 0L;

	public static void startAutoEnterZone(int targetZone)
	{
		if (targetZone < 0) return;
		isAutoEnterZone = true;
		autoEnterTargetZone = targetZone;
		autoEnterTargetMap = TileMap.mapID;
		lastAutoEnterZoneAttempt = 0L;
		notify("⚡ Bắt đầu Auto vào Khu " + targetZone + "...");
	}

	public static void stopAutoEnterZone()
	{
		isAutoEnterZone = false;
		autoEnterTargetZone = -1;
		autoEnterTargetMap = -1;
		notify("🛑 Đã dừng Auto vào khu!");
	}

	private static long lastZoneRequestTime = 0L;
	private static long lastZoneChangeTime = 0L;
	private static long lastZoneAttemptTime = 0L;
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
	public static long lastMapChangeCompletedTime = 0L;
	public static int lastChangeMapFromMapId = -1;
	public static long lastChangeMapSuccessTime = 0L;
	public static Waypoint currentTeleportWaypoint = null;
	public static int teleportTargetX = -1;
	public static int teleportTargetY = -1;
	public static long lastTeleportRequestMapTime = 0L;
	public static int teleportRequestCount = 0;

	// Tương thích ngược: cờ lướt (đã bỏ để dùng dịch tức thời 0ms theo yêu cầu)
	public static bool isGlidingToWaypoint = false;
	public static Waypoint glideWaypoint = null;

	// Bộ đệm vi bước tọa độ và điều phối vị trí (Micro-Step Coordinate Buffer)
	// Về mặt hiển thị, nhân vật luôn dịch tức thời 0ms tới cổng
	public static bool isFastSteppingToWaypoint = false;
	public static Waypoint stepTargetWaypoint = null;
	public static int stepTargetX = 0;
	public static int stepTargetY = 0;
	public static int stepCurrentX = 0;
	public static int stepCurrentY = 0;
	public static long lastStepPacketTime = 0L;
	public static long stepStartTime = 0L;

	public static int lastSpawnX = 0;
	public static int lastSpawnY = 0;
	public static bool hasMovedFromSpawn = false;
	public static Waypoint spawnWaypoint = null;

	public static bool isPointInWaypoint(int x, int y, Waypoint wp)
	{
		if (wp == null) return false;
		if (TileMap.mapID == 47 || TileMap.isInAirMap())
		{
			return (y <= wp.minY + wp.maxY && x > wp.minX && x < wp.maxX);
		}
		return (x >= wp.minX - 12 && x <= wp.maxX + 12 && y >= wp.minY - 12 && y <= wp.maxY + 12);
	}

	public static bool isSpawnWaypointProtection(int cx, int cy)
	{
		long now = mSystem.currentTimeMillis();
		// 1. Bảo vệ chống dội ngược trong 500ms đầu sau khi sang map
		if (now - lastMapChangeCompletedTime < 500L)
		{
			return true;
		}

		// 2. Nếu nhân vật chưa di chuyển ra khỏi vùng cổng vừa tới (cách điểm xuất hiện > 25px)
		if (!hasMovedFromSpawn)
		{
			if (Res.abs(cx - lastSpawnX) < 25 && Res.abs(cy - lastSpawnY) < 25)
			{
				return true;
			}
			hasMovedFromSpawn = true;
		}

		// 3. Nếu nhân vật vẫn đang đứng trong cổng vừa hồi sinh, tiếp tục bảo vệ cho tới khi bước hẳn ra ngoài
		if (spawnWaypoint != null)
		{
			if (isPointInWaypoint(cx, cy, spawnWaypoint))
			{
				return true;
			}
			spawnWaypoint = null;
		}
		return false;
	}

	public static bool isMapChangeCooldown()
	{
		return mSystem.currentTimeMillis() - lastChangeMapSuccessTime < 250L;
	}

	public static void ensureSafeSpawnGround(Char myChar)
	{
		if (myChar == null) return;

		// 1. Chống kẹt vào mép biên hai bên của bản đồ (giữ khoảng cách an toàn tối thiểu 36px)
		if (myChar.cx < 36) myChar.cx = 36;
		if (myChar.cx > TileMap.pxw - 36) myChar.cx = TileMap.pxw - 36;

		// 2. Chống rơi vào góc lag địa hình: Nếu nhân vật bị kẹt bên trong khối đá/vách núi (tile 4 hoặc 8)
		if ((TileMap.tileTypeAtPixel(myChar.cx, myChar.cy - 12) & 4) == 4 || (TileMap.tileTypeAtPixel(myChar.cx, myChar.cy - 12) & 8) == 8)
		{
			for (int offset = 24; offset <= 96; offset += 24)
			{
				if ((TileMap.tileTypeAtPixel(myChar.cx - offset, myChar.cy - 12) & 4) != 4)
				{
					myChar.cx -= offset;
					break;
				}
				if ((TileMap.tileTypeAtPixel(myChar.cx + offset, myChar.cy - 12) & 4) != 4)
				{
					myChar.cx += offset;
					break;
				}
			}
		}

		// 3. Chống đứng im lơ lửng trên trời khi qua map: Tìm mặt đất (tile 2) bên dưới nhân vật để hạ cánh an toàn
		if ((TileMap.tileTypeAtPixel(myChar.cx, myChar.cy) & 2) != 2)
		{
			int groundY = -1;
			for (int y = myChar.cy; y < TileMap.pxh - 12; y += 12)
			{
				if ((TileMap.tileTypeAtPixel(myChar.cx, y) & 2) == 2)
				{
					groundY = y;
					break;
				}
			}

			if (groundY > 0)
			{
				myChar.cy = groundY;
				myChar.statusMe = 1;
				myChar.cvy = 0;
				myChar.delayFall = 0;
			}
			else
			{
				myChar.statusMe = 4;
				myChar.delayFall = 0;
				myChar.cvy = 2;
			}
		}
	}

	public static void triggerManualWaypointChangeMap(Char myChar, Waypoint wp)
	{
		if (myChar == null || wp == null) return;
		if (Char.isLoadingMap || Controller.isStopReadMessage) return;

		long now = mSystem.currentTimeMillis();
		// Debounce 400ms để chống spam liên tục nhưng vẫn phản hồi nhanh
		if (now - lastChangeMapRequestTime < 400L) return;
		lastChangeMapRequestTime = now;

		// 1. Dừng di chuyển chuột nếu có
		myChar.currentMovePoint = null;
		if (GameScr.instance != null)
		{
			GameScr.instance.clickMoving = false;
		}

		// 2. KHÔNG THAY ĐỔI cx, cy CỦA NHÂN VẬT:
		// Tuyệt đối giữ nguyên tọa độ thực tế của nhân vật đang di chuyển.
		// Không bị hút/giật vào cổng và không bị thay đổi độ cao Y làm rơi xuống đáy bản đồ.

		// 3. Ép gửi gói tin di chuyển vị trí hiện tại lên server
		Service.isForceSendMove = true;
		Service.gI().charMove();

		// Không khoá phím hay gán ischangingMap để nhân vật không bị đơ nếu mạng trễ
		Char.isLockKey = false;
		Char.ischangingMap = false;

		lastChangeMapFromMapId = TileMap.mapID;
		sendChangeMapPacket(wp);
	}

	public static void onMapLoaded()
	{
		lastChangeMapSuccessTime = mSystem.currentTimeMillis();
		lastMapChangeCompletedTime = lastChangeMapSuccessTime;
		lastChangeMapFromMapId = TileMap.mapID;
		currentTeleportWaypoint = null;
		isFastSteppingToWaypoint = false;
		stepTargetWaypoint = null;
		isGlidingToWaypoint = false;
		glideWaypoint = null;
		Char.ischangingMap = false;
		Char.isLockKey = false;
		lastChangeMapRequestTime = 0L;
		spawnWaypoint = null;
		if (Char.myCharz() != null)
		{
			ensureSafeSpawnGround(Char.myCharz());
			lastSpawnX = Char.myCharz().cx;
			lastSpawnY = Char.myCharz().cy;
			hasMovedFromSpawn = false;
			Char.myCharz().isTeleport = false;
			Char.myCharz().isLockMove = false;
			Char.myCharz().isLockAttack = false;
			Char.myCharz().cxSend = Char.myCharz().cx;
			Char.myCharz().cySend = Char.myCharz().cy;
			Char.myCharz().currentMovePoint = null;
			Char.myCharz().cvx = 0;

			// Ghi nhận cổng dịch chuyển tại vị trí vừa hồi sinh để tránh bị dội ngược sang map cũ
			if (TileMap.vGo != null)
			{
				for (int i = 0; i < TileMap.vGo.size(); i++)
				{
					Waypoint wp = (Waypoint)TileMap.vGo.elementAt(i);
					if (wp != null && isPointInWaypoint(Char.myCharz().cx, Char.myCharz().cy, wp))
					{
						spawnWaypoint = wp;
						break;
					}
				}
			}

			// Căn chỉnh camera và đồng bộ hóa toàn diện tọa độ tilemap
			GameScr.loadCamera(fullmScreen: false, -1, -1);
		}
		if (GameScr.gI() != null)
		{
			GameScr.gI().isLockKey = false;
		}
		InfoDlg.hide();
		GameCanvas.endDlg();
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

		if (cmd.Equals("tb") || cmd.Equals("boss"))
		{
			BossManager.ToggleBossNotice();
			return true;
		}

		if (cmd.Equals("nk") || cmd.Equals("nguoi") || cmd.Equals("ds"))
		{
			BossManager.ToggleCharInMap();
			return true;
		}

		if (cmd.Equals("ts"))
		{
			isTanSat = !isTanSat;
			if (!isTanSat)
			{
				isAutoTrain = false;
				if (Char.myCharz() != null)
				{
					Char.myCharz().mobFocus = null;
				}
			}
			notify("Tàn sát: " + (isTanSat ? "BẬT" : "TẮT"));
			return true;
		}

		if (cmd.Equals("ak"))
		{
			isAutoTrain = !isAutoTrain;
			if (!isAutoTrain && Char.myCharz() != null)
			{
				Char.myCharz().mobFocus = null;
			}
			notify("Tự luyện tập: " + (isAutoTrain ? "BẬT" : "TẮT"));
			return true;
		}

		if (cmd.Equals("dmo") || cmd.Equals("stopmo"))
		{
			stopAutoOpenItem("Đã dừng tự động mở!");
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

		if (cmd.Equals("qltk") || cmd.Equals("acc") || cmd.Equals("tk"))
		{
			showAccountSelectorMenu();
			return true;
		}

		if (cmd.Equals("xoadata") || cmd.Equals("cleardata") || cmd.Equals("taidata") || cmd.Equals("updata"))
		{
			Rms.clearAll();
			GameCanvas.startOK("Đã xóa cache dữ liệu! Vui lòng khởi động lại game để cập nhật đồ họa mới.", 8885, null);
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

		// Lệnh Xmap & Goback
		if (cmd.Equals("xmap"))
		{
			XmapManager.showXmapMenu();
			return true;
		}

		if (cmd.StartsWith("xmap "))
		{
			string arg = text.Substring(5).Trim();
			int targetId = XmapManager.findMapIdByName(arg);
			if (targetId != -1)
			{
				XmapManager.startXmap(targetId);
			}
			else
			{
				notify("Không tìm thấy map: " + arg);
			}
			return true;
		}

		if (cmd.Equals("cxmap") || cmd.Equals("stopxmap"))
		{
			XmapManager.stopXmap();
			return true;
		}

		if (cmd.Equals("cgb") || cmd.Equals("caigoback") || cmd.Equals("gbmenu") || cmd.Equals("mgb"))
		{
			showGobackDialog();
			return true;
		}

		if (cmd.Equals("gb") || cmd.Equals("goback"))
		{
			XmapManager.toggleGoback();
			return true;
		}

		if (cmd.Equals("gk") || cmd.Equals("gobackki") || cmd.Equals("cgk"))
		{
			XmapManager.toggleGobackKi();
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

		// Tốc độ xử lý khung hình: s1 -> s10 (hoặc bất kỳ giá trị nào từ 1 đến 10)
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
			Application.targetFrameRate = GraphicsOptimizer.currentActualTargetFps;
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

			BossManager.Update();

			if (currentSpeed > 0f && Time.timeScale != currentSpeed)
			{
				Time.timeScale = currentSpeed;
			}

			if (GameCanvas.gameTick % 30 == 0)
			{
				Char.myCharz().updateSpeed();
			}

			long now = mSystem.currentTimeMillis();

			// Tự động kích hoạt lựa chọn Hợp thể / Tách hợp thể khi Menu Server mở ra (0 delay)
			if (isPendingHopThe)
			{
				checkAutoHopTheMenu();
			}

			// Tự động thử lại hợp thể / tách hợp thể ngay khi hết cooldown (đánh lừa server không trễ 1 mili-giây)
			if (isAutoRetryHopThe)
			{
				Char myChar = Char.myCharz();
				bool currentFused = (myChar != null && (myChar.isNhapThe || myChar.isFusion));
				if (currentFused != initialFusedStateWhenPressedF)
				{
					// Đã thay đổi trạng thái hợp thể / tách hợp thể thành công!
					isAutoRetryHopThe = false;
					isPendingHopThe = false;
				}
				else if (now > autoRetryHopTheUntil)
				{
					isAutoRetryHopThe = false;
				}
				else if (now - lastHopTheRequestTime >= 250L)
				{
					lastHopTheRequestTime = now;
					sendHopTheItemUse();
				}
			}

			// Tắt hẳn autoPlay mặc định của game để không bị xung đột, giật cục, loạn mục tiêu
			GameScr.isAutoPlay = false;

			// Tự động giải phóng khóa mục tiêu nếu đổi map hoặc đổi khu
			if (TileMap.mapID != lastTrackedMapId || TileMap.zoneID != lastTrackedZoneId)
			{
				lastTrackedMapId = TileMap.mapID;
				lastTrackedZoneId = TileMap.zoneID;
				if (lockedTargetCharId != -1)
				{
					lockedTargetCharId = -1;
					lockedTargetCharName = "";
					if (Char.myCharz() != null)
					{
						Char.myCharz().charFocus = null;
						Char.myCharz().mobFocus = null;
						Char.myCharz().npcFocus = null;
						Char.myCharz().itemFocus = null;
					}
				}
			}

			// Duy trì khóa mục tiêu người chơi theo ID (Phím I)
			if (lockedTargetCharId != -1)
			{
				Char myChar = Char.myCharz();
				if (myChar != null)
				{
					Char target = GameScr.findCharInMap(lockedTargetCharId);
					if (target != null && target.statusMe != 14 && target.statusMe != 5)
					{
						myChar.charFocus = target;
						myChar.mobFocus = null;
						myChar.npcFocus = null;
						myChar.itemFocus = null;
					}
					else if (target == null)
					{
						// Target đã rời map -> Tự động hủy khóa để tránh bị treo
						lockedTargetCharId = -1;
						lockedTargetCharName = "";
						myChar.charFocus = null;
						myChar.mobFocus = null;
						notify("Mục tiêu đã rời map! Đã tự động hủy khóa.");
					}
				}
			}


			// 0. Auto Vào Khu liên tục cho đến khi vào được khu chỉ định
			if (isAutoEnterZone)
			{
				if (TileMap.mapID != autoEnterTargetMap)
				{
					stopAutoEnterZone();
				}
				else if (TileMap.zoneID == autoEnterTargetZone)
				{
					isAutoEnterZone = false;
					autoEnterTargetZone = -1;
					autoEnterTargetMap = -1;
					notify("🎉 ĐÃ VÀO THÀNH CÔNG KHU " + TileMap.zoneID + "!");
				}
				else if (Char.myCharz().cHP > 0 && Char.myCharz().statusMe != 14)
				{
					if (now - lastAutoEnterZoneAttempt >= 1200L)
					{
						lastAutoEnterZoneAttempt = now;
						Service.gI().requestChangeZone(autoEnterTargetZone, -1);
					}
				}
			}

			// 1. Tự động dùng Trị thương, Đẻ trứng, Buff khi HP/KI xuống thấp hoặc hết đệ (kiểm tra mỗi 120ms)
			if (now - lastAutoBuffCheckTime >= 120L)
			{
				lastAutoBuffCheckTime = now;
				checkAutoBuffOrHeal(now);
			}

			if (!isTanSat && !isAutoTrain)
			{
				GameScr.isAutoPlay = false;
			}

			// 2. Tàn Sát quái trong map (chạy mỗi 35ms thay vì mỗi frame, giảm 70% tải CPU)
			if (isTanSat && now - lastTanSatUpdateTime >= 35L)
			{
				lastTanSatUpdateTime = now;
				updateTanSat(now);
			}
			// 3. TDLT là bùa hỗ trợ exp và di chuyển: TUYỆT ĐỐI KHÔNG TỰ ĐỘNG ĐÁNH QUÁI khi Tàn Sát đang tắt!

			// 4. Tự động đổi khu khi hết quái (chạy độc lập, kiểm tra mỗi 250ms)
			if (isAutoChangeZone && now - lastAutoChangeZoneCheckTime >= 250L)
			{
				lastAutoChangeZoneCheckTime = now;
				updateAutoChangeZone(now);
			}

			// 5. Auto Item (Đồ ăn, Cuồng nộ, Tự động luyện tập, kiểm tra mỗi 500ms)
			if (isAutoItem && now - lastAutoItemCheckTime >= 500L)
			{
				lastAutoItemCheckTime = now;
				updateAutoItem(now);
			}

			// 6. Tự động nhặt vật phẩm & Bùa thu hút (kiểm tra mỗi 80ms)
			if ((isAutoPick || isBuaThuHut) && now - lastAutoPickCheckTime >= 80L)
			{
				lastAutoPickCheckTime = now;
				updateAutoPick(now);
			}

			// 7. Tự động vứt vật phẩm theo D/S ItemID
			if (isAutoDrop)
			{
				updateAutoDrop(now);
			}

			// 7.1. Tự động mở vật phẩm
			if (isAutoOpeningItem)
			{
				updateAutoOpenItem(now);
			}

			// 8. Quản lý trạng thái chuyển map
			if (TileMap.mapID != lastChangeMapFromMapId && !GameCanvas.isLoading && !Char.isLoadingMap)
			{
				currentTeleportWaypoint = null;
				onMapLoaded();
			}
			else if (Char.ischangingMap)
			{
				if (lastChangeMapRequestTime > 0L && now - lastChangeMapRequestTime > 4000L)
				{
					isFastSteppingToWaypoint = false;
					stepTargetWaypoint = null;
					Char.ischangingMap = false;
					Char.isLockKey = false;
					InfoDlg.hide();
					currentTeleportWaypoint = null;
					lastChangeMapRequestTime = 0L;
				}
			}

			// 9. Cập nhật hệ thống Xmap & Tự động Goback khi chết
			XmapManager.update(now);

		}
		catch (Exception)
		{
		}
	}

	// =============================================================
	// TÍNH NĂNG DỊCH MAP TỨC THÌ BẰNG PHÍM J, K, L (CHUẨN KHÔNG LAG/DISCONNECT)
	// =============================================================
	// HỆ THỐNG PHÍM TẮT NHANH SIÊU TỐC (M, K, T, F, C, N, I, J, L, F2)
	// =============================================================
	// =============================================================
	// HỆ THỐNG PHÍM TẮT NHANH SIÊU TỐC (M, K, T, F, C, N, I, J, L, F2)
	// =============================================================
	public static int lockedTargetCharId = -1;
	public static string lockedTargetCharName = "";
	private static int lastTrackedMapId = -1;
	private static int lastTrackedZoneId = -1;

	public static void toggleLockTargetChar()
	{
		Char myChar = Char.myCharz();
		if (myChar == null) return;

		// Nếu đang khóa -> Bấm I để hủy khóa sạch sẽ, xóa toàn bộ cache focus
		if (lockedTargetCharId != -1)
		{
			notify("Đã hủy khóa mục tiêu: " + lockedTargetCharName);
			lockedTargetCharId = -1;
			lockedTargetCharName = "";
			myChar.charFocus = null;
			myChar.mobFocus = null;
			myChar.npcFocus = null;
			myChar.itemFocus = null;
			return;
		}

		Char target = null;

		// 1. Ưu tiên mục tiêu đang chọn bằng click / tab (charFocus)
		if (myChar.charFocus != null && myChar.charFocus != myChar)
		{
			target = myChar.charFocus;
		}

		// 2. Nếu chưa có charFocus, tự động tìm người chơi mà chuột đang chỉ trỏ vào trên màn hình
		if (target == null && GameScr.vCharInMap != null)
		{
			try
			{
				int mouseWorldX = (int)(Input.mousePosition.x / (float)mGraphics.zoomLevel) + GameScr.cmx;
				int mouseWorldY = (int)(((float)Screen.height - Input.mousePosition.y) / (float)mGraphics.zoomLevel) + GameScr.cmy;
				for (int i = 0; i < GameScr.vCharInMap.size(); i++)
				{
					Char c = (Char)GameScr.vCharInMap.elementAt(i);
					if (c != null && c != myChar)
					{
						int cx = c.cx;
						int cy = c.cy;
						if (mouseWorldX >= cx - 35 && mouseWorldX <= cx + 35 && mouseWorldY >= cy - 70 && mouseWorldY <= cy + 25)
						{
							target = c;
							break;
						}
					}
				}
			}
			catch (Exception)
			{
			}
		}

		// 3. Tiến hành khóa mục tiêu
		if (target != null)
		{
			lockedTargetCharId = target.charID;
			lockedTargetCharName = target.cName;
			myChar.charFocus = target;
			myChar.mobFocus = null;
			notify("Đã khóa mục tiêu: " + lockedTargetCharName + " [ID: " + lockedTargetCharId + "]");
		}
		else
		{
			notify("Hãy trỏ/click vào người chơi trước rồi bấm phím I để khóa!");
		}
	}

	public static void toggleAutoTrain()
	{
		try
		{
			int bagIdx = findAutoTrainBagIndex();
			if (bagIdx >= 0)
			{
				bool currentlyActive = isAutoTrainBuffActive() || isAutoTrain;
				if (currentlyActive)
				{
					isAutoTrain = false;
					GameScr.canAutoPlay = false;
					if (Char.vItemTime != null)
					{
						for (int j = Char.vItemTime.size() - 1; j >= 0; j--)
						{
							ItemTime t = (ItemTime)Char.vItemTime.elementAt(j);
							if (t != null && (t.idIcon == 4387 || t.idIcon == 4028 || t.idIcon == 521 || t.idIcon == 4330))
							{
								Char.vItemTime.removeElementAt(j);
							}
						}
					}
					Service.gI().useItem(0, 1, (sbyte)bagIdx, -1);
					if (itemCol1Selected != null && itemCol1Selected.Length > 11)
					{
						itemCol1Selected[11] = false;
					}
					saveSettings();
					if (Char.myCharz() != null)
					{
						Char.myCharz().mobFocus = null;
					}
					notify("Đã TẮT Tự Động Luyện Tập (TDLT)");
				}
				else
				{
					isAutoTrain = true;
					GameScr.canAutoPlay = true;
					Service.gI().useItem(0, 1, (sbyte)bagIdx, -1);
					if (itemCol1Selected != null && itemCol1Selected.Length > 11)
					{
						itemCol1Selected[11] = true;
					}
					saveSettings();
					notify("Đã BẬT Tự Động Luyện Tập (TDLT)");
				}
			}
			else
			{
				if (isAutoTrain || isAutoTrainBuffActive())
				{
					isAutoTrain = false;
					GameScr.canAutoPlay = false;
					notify("Đã TẮT Tự Động Luyện Tập (TDLT)");
				}
				else
				{
					notify("Không tìm thấy Tự Động Luyện Tập (item 521) trong hành trang!");
				}
			}
		}
		catch (Exception)
		{
		}
	}

	// Tự động nhận diện và kích hoạt lựa chọn Hợp Thể / Tách Hợp Thể ngay khi Menu Server gửi về (0 delay)
	public static bool handleAutoHopTheFromMenuItems(MyVector items)
	{
		if (items == null || items.size() == 0) return false;

		Char myChar = Char.myCharz();
		bool isFused = (myChar != null && (myChar.isNhapThe || myChar.isFusion));
		Command targetCmd = null;

		for (int m = 0; m < items.size(); m++)
		{
			Command cmd = (Command)items.elementAt(m);
			if (cmd != null && !string.IsNullOrEmpty(cmd.caption))
			{
				string cap = cmd.caption.ToLower();
				if (isFused)
				{
					if (cap.Contains("tách") || cap.Contains("tach") || cap.Contains("tháo") || cap.Contains("thao") || cap.Contains("bỏ") || cap.Contains("bo") || cap.Contains("hủy") || cap.Contains("huy"))
					{
						targetCmd = cmd;
						break;
					}
				}
				else
				{
					if (cap.Contains("vĩnh viễn") || cap.Contains("vinh vien")) continue;
					if (cap.Contains("porata") || cap.Contains("c2") || cap.Contains("cấp 2") || cap.Contains("hợp thể") || cap.Contains("hop the"))
					{
						targetCmd = cmd;
						if (cap.Contains("c2") || cap.Contains("cấp 2") || cap.Contains("porata")) break;
					}
				}
			}
		}

		if (targetCmd != null)
		{
			isPendingHopThe = false;
			isAutoRetryHopThe = false;
			targetCmd.performAction();
			// KHÔNG gọi notify để tránh hiện bóng chat đè lên đầu nhân vật
			return true;
		}

		return false;
	}

	public static void checkAutoHopTheMenu()
	{
		if (!isPendingHopThe) return;

		long now = mSystem.currentTimeMillis();
		if (now - pendingHopTheTime > 1500L)
		{
			isPendingHopThe = false;
			return;
		}

		if (GameCanvas.menu != null && GameCanvas.menu.showMenu && GameCanvas.menu.menuItems != null && GameCanvas.menu.menuItems.size() > 0)
		{
			if (handleAutoHopTheFromMenuItems(GameCanvas.menu.menuItems))
			{
				GameCanvas.menu.showMenu = false;
			}
		}
	}

	// Quét và gửi lệnh dùng Bông tai (Ưu tiên Bông tai cấp 2 trước, thường sau; ưu tiên trên người trước, túi sau)
	public static bool sendHopTheItemUse()
	{
		Char myChar = Char.myCharz();
		if (myChar == null) return false;

		// 1. Quét Bông tai Porata CẤP 2 (ID 921, 1153) trên người (không có thời gian chờ cooldown)
		if (myChar.arrItemBody != null)
		{
			for (int j = 0; j < myChar.arrItemBody.Length; j++)
			{
				Item it = myChar.arrItemBody[j];
				if (it != null && it.template != null)
				{
					int id = it.template.id;
					string name = (it.template.name != null) ? it.template.name.ToLower() : "";
					if (id == 921 || id == 1153 || name.Contains("c2") || name.Contains("cấp 2"))
					{
						isPendingHopThe = true;
						pendingHopTheTime = mSystem.currentTimeMillis();
						sbyte sendIdx = (sbyte)((it.indexUI >= 0) ? it.indexUI : j);
						Service.gI().useItem(0, 0, sendIdx, -1);
						return true;
					}
				}
			}
		}

		// 2. Quét Bông tai Porata CẤP 2 (ID 921, 1153) trong hành trang
		if (myChar.arrItemBag != null)
		{
			for (int i = 0; i < myChar.arrItemBag.Length; i++)
			{
				Item it2 = myChar.arrItemBag[i];
				if (it2 != null && it2.template != null)
				{
					int id2 = it2.template.id;
					string name2 = (it2.template.name != null) ? it2.template.name.ToLower() : "";
					if (id2 == 921 || id2 == 1153 || name2.Contains("c2") || name2.Contains("cấp 2"))
					{
						isPendingHopThe = true;
						pendingHopTheTime = mSystem.currentTimeMillis();
						sbyte sendIdx2 = (sbyte)((it2.indexUI >= 0) ? it2.indexUI : i);
						Service.gI().useItem(0, 1, sendIdx2, -1);
						return true;
					}
				}
			}
		}

		// 3. Quét Bông tai Porata thường (ID 454) trên người
		if (myChar.arrItemBody != null)
		{
			for (int j2 = 0; j2 < myChar.arrItemBody.Length; j2++)
			{
				Item it3 = myChar.arrItemBody[j2];
				if (it3 != null && it3.template != null)
				{
					int id3 = it3.template.id;
					string name3 = (it3.template.name != null) ? it3.template.name.ToLower() : "";
					if (id3 == 454 || name3.Contains("bông tai") || name3.Contains("porata"))
					{
						isPendingHopThe = true;
						pendingHopTheTime = mSystem.currentTimeMillis();
						sbyte sendIdx3 = (sbyte)((it3.indexUI >= 0) ? it3.indexUI : j2);
						Service.gI().useItem(0, 0, sendIdx3, -1);
						return true;
					}
				}
			}
		}

		// 4. Quét Bông tai Porata thường (ID 454) trong hành trang
		if (myChar.arrItemBag != null)
		{
			for (int i2 = 0; i2 < myChar.arrItemBag.Length; i2++)
			{
				Item it4 = myChar.arrItemBag[i2];
				if (it4 != null && it4.template != null)
				{
					int id4 = it4.template.id;
					string name4 = (it4.template.name != null) ? it4.template.name.ToLower() : "";
					if (id4 == 454 || name4.Contains("bông tai") || name4.Contains("porata"))
					{
						isPendingHopThe = true;
						pendingHopTheTime = mSystem.currentTimeMillis();
						sbyte sendIdx4 = (sbyte)((it4.indexUI >= 0) ? it4.indexUI : i2);
						Service.gI().useItem(0, 1, sendIdx4, -1);
						return true;
					}
				}
			}
		}

		return false;
	}

	public static void toggleHopThe()
	{
		try
		{
			Char myChar = Char.myCharz();
			if (myChar == null) return;

			// Tạm ngắt skill đánh 40ms để server rảnh rỗi tiếp nhận lệnh
			pauseAttackUntil = mSystem.currentTimeMillis() + 40L;
			myChar.skillPaint = null;
			myChar.dart = null;
			myChar.isCharge = false;
			myChar.isAttack = false;
			// TUYỆT ĐỐI KHÔNG can thiệp cvx, cvy, currentMovePoint để nhân vật không bị khựng lại!

			// 1. Nếu đang có menu Server hiện lên với lựa chọn Hợp thể / Tách hợp thể, kích hoạt ngay lập tức
			if (GameCanvas.menu != null && GameCanvas.menu.showMenu && GameCanvas.menu.menuItems != null)
			{
				if (handleAutoHopTheFromMenuItems(GameCanvas.menu.menuItems))
				{
					GameCanvas.menu.showMenu = false;
					isAutoRetryHopThe = false;
					return;
				}
			}

			// Dọn dẹp dialog tạm nếu có
			if (GameCanvas.currentDialog != null && GameCanvas.currentDialog is InfoDlg)
			{
				GameCanvas.currentDialog = null;
			}
			InfoDlg.hide();
			Char.isLockKey = false;

			initialFusedStateWhenPressedF = (myChar.isNhapThe || myChar.isFusion);
			isAutoRetryHopThe = true;
			autoRetryHopTheUntil = mSystem.currentTimeMillis() + 11500L;
			lastHopTheRequestTime = mSystem.currentTimeMillis();

			if (!sendHopTheItemUse())
			{
				isAutoRetryHopThe = false;
			}
		}
		catch (Exception)
		{
		}
	}

	public static void useCapsule()
	{
		try
		{
			Char myChar = Char.myCharz();
			if (myChar == null || myChar.arrItemBag == null) return;

			// Hủy cờ hợp thể nếu có để tránh nuốt menu của Capsule
			isPendingHopThe = false;
			isAutoRetryHopThe = false;

			// Tạm ngắt tấn công 20ms để server ở trạng thái rảnh rỗi mở menu Capsule ngay lập tức
			pauseAttackUntil = mSystem.currentTimeMillis() + 20L;
			myChar.skillPaint = null;
			myChar.dart = null;
			myChar.isCharge = false;
			myChar.isAttack = false;
			// TUYỆT ĐỐI KHÔNG can thiệp cvx, cvy, currentMovePoint để nhân vật di chuyển mượt mà không khựng lại!

			int capIdx = -1;

			// Ưu tiên 1: Capsule Đặc Biệt / Kì Bí (ID 194 - dùng vô hạn lần)
			for (int i = 0; i < myChar.arrItemBag.Length; i++)
			{
				Item it = myChar.arrItemBag[i];
				if (it != null && it.template != null && it.template.id == 194)
				{
					capIdx = (it.indexUI >= 0) ? it.indexUI : i;
					break;
				}
			}

			// Ưu tiên 2: Capsule thường (ID 193), Capsule ngày (ID 390-393, 381-385) hoặc tên chứa capsule/capsu/bay
			if (capIdx == -1)
			{
				for (int i = 0; i < myChar.arrItemBag.Length; i++)
				{
					Item it = myChar.arrItemBag[i];
					if (it != null && it.template != null)
					{
						int id = it.template.id;
						string name = (it.template.name != null) ? it.template.name.ToLower() : "";
						if (id == 193 || (id >= 390 && id <= 393) || (id >= 381 && id <= 385) || name.Contains("capsule") || name.Contains("capsu") || name.Contains("bay"))
						{
							capIdx = (it.indexUI >= 0) ? it.indexUI : i;
							break;
						}
					}
				}
			}

			if (capIdx >= 0)
			{
				// Dọn dẹp dialog, panel khác (nếu không phải là bảng chọn map)
				if (GameCanvas.panel != null && GameCanvas.panel.isShow && GameCanvas.panel.type != 14)
				{
					GameCanvas.panel.hide();
				}
				if (GameCanvas.menu != null)
				{
					GameCanvas.menu.showMenu = false;
				}
				if (GameCanvas.currentDialog != null && GameCanvas.currentDialog is InfoDlg)
				{
					GameCanvas.currentDialog = null;
				}
				InfoDlg.hide();
				Char.isLockKey = false;
				Char.ischangingMap = false;

				Service.gI().useItem(0, 1, (sbyte)capIdx, -1);
				// TUYỆT ĐỐI KHÔNG gọi notify("🚀 Mở ... để bay...") để không có bóng chat gây cảm giác delay!
			}
		}
		catch (Exception)
		{
		}
	}

	public static bool isUserTyping()
	{
		try
		{
			// 1. Khung chat trong game (ChatTextField)
			if (ChatTextField.isShowing) return true;
			if (ChatTextField.gI() != null && ChatTextField.gI().isShow) return true;

			// 2. Chat riêng / chat clan trong Panel
			if (GameCanvas.panel != null && GameCanvas.panel.chatTField != null && GameCanvas.panel.chatTField.isShow) return true;

			// 3. Hộp thoại nhập số / text InputDlg của Game (nhập giftcode, nạp ngọc, nhập số lượng...)
			if (GameCanvas.currentDialog != null && GameCanvas.currentDialog is InputDlg) return true;

			// 4. Màn hình nhập quà tặng ClientInput
			if (GameCanvas.currentScreen != null && GameCanvas.currentScreen is Assets.src.g.ClientInput) return true;

			// 5. Ô nhập liệu nội bộ của Mod (chỉ khi người dùng click vào ô để nhập: activeInputIndex >= 0)
			if (activeInputIndex >= 0) return true;
		}
		catch (Exception)
		{
		}
		return false;
	}

	public static void ForceClosePanel()
	{
		try
		{
			if (GameCanvas.panel != null && GameCanvas.panel.isShow)
			{
				GameCanvas.panel.isClose = true;
				GameCanvas.panel.isShow = false;
				GameCanvas.panel.hideNow();
			}
		}
		catch (Exception)
		{
		}
	}

	public static bool isModHotkey(KeyCode key)
	{
		if (isUserTyping()) return false;
		return key == KeyCode.Escape || key == KeyCode.X || key == KeyCode.F2 ||
		       key == KeyCode.C || key == KeyCode.M || key == KeyCode.N ||
		       key == KeyCode.Z || key == KeyCode.P || key == KeyCode.F ||
		       key == KeyCode.B || key == KeyCode.T || key == KeyCode.I ||
		       key == KeyCode.J || key == KeyCode.K || key == KeyCode.L;
	}

	public static bool isCapsuleOpen()
	{
		try
		{
			if (GameCanvas.menu != null && GameCanvas.menu.showMenu)
			{
				return true;
			}
			if (GameCanvas.panel != null && GameCanvas.panel.isShow && (GameCanvas.panel.type == 14 || GameCanvas.panel.type == 12))
			{
				return true;
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	public static void toggleCapsule()
	{
		try
		{
			if (isCapsuleOpen())
			{
				if (GameCanvas.menu != null && GameCanvas.menu.showMenu)
				{
					GameCanvas.menu.showMenu = false;
				}
				ForceClosePanel();
			}
			else
			{
				useCapsule();
			}
		}
		catch (Exception)
		{
		}
	}

	public static void toggleZone()
	{
		try
		{
			if (GameCanvas.panel != null && GameCanvas.panel.isShow && GameCanvas.panel.type == 3)
			{
				ForceClosePanel();
			}
			else
			{
				closeAllDialogs();
				if (GameCanvas.menu != null && GameCanvas.menu.showMenu) GameCanvas.menu.showMenu = false;
				ForceClosePanel();
				Service.gI().openUIZone();
			}
		}
		catch (Exception)
		{
		}
	}

	public static void toggleXmap()
	{
		try
		{
			bool isXmapOpen = (GameCanvas.menu != null && GameCanvas.menu.showMenu) ||
			                  (GameCanvas.panel != null && GameCanvas.panel.isShow && (GameCanvas.panel.type == 14 || Pk9rXmap.IsMapTransAsXmap)) ||
			                  XmapManager.isShowXmapDialog;

			if (isXmapOpen)
			{
				if (GameCanvas.menu != null && GameCanvas.menu.showMenu) GameCanvas.menu.showMenu = false;
				ForceClosePanel();
				Pk9rXmap.IsMapTransAsXmap = false;
				XmapManager.isShowXmapDialog = false;
				return;
			}

			if (Pk9rXmap.IsXmapRunning)
			{
				XmapController.FinishXmap();
				GameScr.info1.addInfo("Đã huỷ Xmap", 0);
				return;
			}

			closeAllDialogs();
			ForceClosePanel();
			XmapController.ShowXmapMenu();
		}
		catch (Exception)
		{
		}
	}

	public static void toggleAutoPick()
	{
		try
		{
			if (isShowPickDialog)
			{
				closeAllDialogs();
			}
			else
			{
				closeAllDialogs();
				showAutoPickMenu();
			}
		}
		catch (Exception)
		{
		}
	}

	public static void toggleModMenu()
	{
		try
		{
			if (isAnyDialogOpen)
			{
				closeAllDialogs();
			}
			else
			{
				showMainControlDialog();
			}
		}
		catch (Exception)
		{
		}
	}

	public static void togglePanelProfile()
	{
		try
		{
			if (GameCanvas.panel != null && GameCanvas.panel.isShow && GameCanvas.panel.type == 0)
			{
				ForceClosePanel();
			}
			else
			{
				closeAllDialogs();
				if (GameCanvas.menu != null && GameCanvas.menu.showMenu) GameCanvas.menu.showMenu = false;
				GameCanvas.panel.setTypeMain();
				GameCanvas.panel.show();
			}
		}
		catch (Exception)
		{
		}
	}

	private static KeyCode lastHotkeyCode = KeyCode.None;

	public static bool handleHotkey(KeyCode key)
	{
		try
		{
			if (!isModHotkey(key)) return false;

			// 0. Nếu đang gõ văn bản trong ô input (chat, giftcode, ô nhập mod), không nhận phím tắt chữ cái
			if (isUserTyping())
			{
				if (key == KeyCode.Escape)
				{
					if (ChatTextField.gI() != null && ChatTextField.gI().isShow) ChatTextField.gI().isShow = false;
					if (GameCanvas.currentDialog != null && GameCanvas.currentDialog is InputDlg) GameCanvas.currentDialog = null;
					activeInputIndex = -1;
					return true;
				}
				return false;
			}

			// Chống dội phím (debounce 160ms cho cùng 1 phím) để 1 lần gõ nhẹ là ăn ngay lập tức, không bị dính phím mở rồi tắt ngay
			long now = mSystem.currentTimeMillis();
			if (key == lastHotkeyCode && now - lastHotkeyActionTime < 160L)
			{
				return true;
			}
			lastHotkeyCode = key;
			lastHotkeyActionTime = now;

			// 1. Phím Escape: Đóng NGAY LẬP TỨC bất cứ menu, popup sự kiện, dialog mod, panel hay tiến trình Xmap nào
			if (key == KeyCode.Escape)
			{
				if (ChatPopup.serverChatPopUp != null)
				{
					ChatPopup.CloseServerPopup();
					return true;
				}
				if (isAnyDialogOpen)
				{
					closeAllDialogs();
					return true;
				}
				ForceClosePanel();
				if (GameCanvas.menu != null && GameCanvas.menu.showMenu)
				{
					GameCanvas.menu.showMenu = false;
					return true;
				}
				if (Pk9rXmap.IsXmapRunning)
				{
					XmapController.FinishXmap();
					GameScr.info1.addInfo("Đã huỷ Xmap", 0);
					return true;
				}
				return false;
			}

			// 2. Phím X / F2: Tắt khi mở, Mở khi tắt Menu Cài Đặt Mod (1 bấm ăn ngay lập tức)
			if (key == KeyCode.X || key == KeyCode.F2)
			{
				toggleModMenu();
				return true;
			}

			// 3. Phím N: Tắt khi mở, Mở khi tắt Bảng Cài Đặt Auto Nhặt & Vứt (1 bấm ăn ngay)
			if (key == KeyCode.N)
			{
				toggleAutoPick();
				return true;
			}

			// 4. Phím C: Tắt khi mở, Mở khi tắt Capsule Bay (đóng dứt khoát 1 lần không qua tab nhiệm vụ)
			if (key == KeyCode.C)
			{
				toggleCapsule();
				return true;
			}

			// 5. Phím M: Tắt khi mở, Mở khi tắt Bảng Đổi Khu (đóng dứt khoát 1 lần không qua tab nhiệm vụ)
			if (key == KeyCode.M)
			{
				toggleZone();
				return true;
			}

			// 6. Phím Z: Tắt khi mở, Mở khi tắt Bảng Xmap (Dịch chuyển map tức thời)
			if (key == KeyCode.Z)
			{
				toggleXmap();
				return true;
			}

			// 7. Phím P: Tắt khi mở, Mở khi tắt Profile & Hành trang
			if (key == KeyCode.P)
			{
				togglePanelProfile();
				return true;
			}

			// 8. Phím F hoặc B: Auto Hợp thể / Tách hợp thể (Bông tai Porata)
			if (key == KeyCode.F || key == KeyCode.B)
			{
				toggleHopThe();
				return true;
			}

			// 9. Phím T: Bật / Tắt Tự Luyện Tập (TDLT)
			if (key == KeyCode.T)
			{
				toggleAutoTrain();
				return true;
			}

			// 10. Phím I: Khóa / Hủy khóa mục tiêu đang chỉ định
			if (key == KeyCode.I)
			{
				toggleLockTargetChar();
				return true;
			}

			// 11. Phím J: Dịch map TRÁI
			if (key == KeyCode.J)
			{
				handleQuickMapChange(0);
				return true;
			}

			// 12. Phím K: Dịch map GIỮA
			if (key == KeyCode.K)
			{
				handleQuickMapChange(1);
				return true;
			}

			// 13. Phím L: Dịch map PHẢI
			if (key == KeyCode.L)
			{
				handleQuickMapChange(2);
				return true;
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	public static void checkHotkeysEveryFrame()
	{
		try
		{
			// Tự động kích hoạt lựa chọn Hợp thể / Tách hợp thể khi Menu Server mở ra (0 frame delay)
			if (isPendingHopThe)
			{
				checkAutoHopTheMenu();
			}

			// 0. Duy trì khóa focus liên tục từng khung hình nếu đã khóa mục tiêu
			if (lockedTargetCharId != -1)
			{
				Char myChar = Char.myCharz();
				if (myChar != null)
				{
					Char target = GameScr.findCharInMap(lockedTargetCharId);
					if (target != null && target.statusMe != 14 && target.statusMe != 5)
					{
						myChar.charFocus = target;
						myChar.mobFocus = null;
						myChar.npcFocus = null;
						myChar.itemFocus = null;
					}
					else if (target == null)
					{
						lockedTargetCharId = -1;
						lockedTargetCharName = "";
						myChar.charFocus = null;
						myChar.mobFocus = null;
					}
				}
			}

			// Tự động giải phóng trạng thái kẹt đơ khi qua map (Unfreeze Safety Guard)
			Char myCharUnfreeze = Char.myCharz();
			if (myCharUnfreeze != null && GameCanvas.currentScreen is GameScr)
			{
				long nowTime = mSystem.currentTimeMillis();
				// 1. Chống kẹt hiệu ứng tàu bay isTeleport
				if (myCharUnfreeze.isTeleport && (nowTime - lastChangeMapSuccessTime > 1200L || nowTime - lastChangeMapRequestTime > 2000L))
				{
					myCharUnfreeze.isTeleport = false;
					myCharUnfreeze.statusMe = 1;
					myCharUnfreeze.cvy = 0;
					myCharUnfreeze.cvx = 0;
					myCharUnfreeze.cy = findGroundY(myCharUnfreeze.cx);
					Service.gI().charMove();
				}


				// 2. Chống kẹt cờ đổi map ischangingMap (3.5s an toàn)
				if (Char.ischangingMap)
				{
					if (lastChangeMapRequestTime == 0L)
					{
						lastChangeMapRequestTime = nowTime;
					}
					else if (nowTime - lastChangeMapRequestTime > 3500L)
					{
						Char.ischangingMap = false;
						Char.isLockKey = false;
						InfoDlg.hide();
						lastChangeMapRequestTime = 0L;
					}
				}

				// Chống kẹt cờ nạp map (Char.isLoadingMap) gây màn hình đen / đơ game
				if (Char.isLoadingMap && lastChangeMapRequestTime > 0L && (nowTime - lastChangeMapRequestTime > 3500L))
				{
					Char.isLoadingMap = false;
					GameCanvas.isLoading = false;
				}

				// 3. Mở khóa phím di chuyển nếu không đang trong quá trình chuyển map hoặc lướt
				if (!Char.ischangingMap && !isGlidingToWaypoint)
				{
					if (Char.isLockKey && !isUserTyping() && (GameCanvas.menu == null || !GameCanvas.menu.showMenu))
					{
						Char.isLockKey = false;
					}
					if (GameScr.gI() != null && GameScr.gI().isLockKey && !isUserTyping() && (GameCanvas.menu == null || !GameCanvas.menu.showMenu))
					{
						GameScr.gI().isLockKey = false;
					}
				}

				// 4. Mở khóa InfoDlg nếu bị treo (4.0s an toàn)
				if (InfoDlg.isLock && !isUserTyping() && lastChangeMapRequestTime > 0L && (nowTime - lastChangeMapRequestTime > 4000L))
				{
					InfoDlg.hide();
				}

				// Cập nhật trạng thái đã rời cổng hồi sinh để cho phép đổi map tự động sau này
				if (!hasMovedFromSpawn)
				{
					if (Res.abs(myCharUnfreeze.cx - lastSpawnX) > 36 || Res.abs(myCharUnfreeze.cy - lastSpawnY) > 36)
					{
						hasMovedFromSpawn = true;
					}
				}
			}

			// Tất cả các phím tắt Mod (Escape, X, F2, C, M, N, Z, P, F, B, T, I, J, K, L) 
			// đều được điều phối tập trung trực tiếp từ Main.OnGUI() qua handleHotkey() 
			// để đảm bảo 1 lần chạm là phản hồi ngay lập tức (0ms delay), không bị dội phím.
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

	public static int findAutoTrainBagIndex()
	{
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.arrItemBag == null) return -1;
		for (int i = 0; i < myChar.arrItemBag.Length; i++)
		{
			Item it = myChar.arrItemBag[i];
			if (it != null && it.template != null)
			{
				if (it.template.id == 521 || it.template.iconID == 4387 || it.template.iconID == 4028 || it.template.iconID == 521 || it.template.iconID == 4330)
				{
					return i;
				}
				if (!string.IsNullOrEmpty(it.template.name))
				{
					string name = it.template.name.ToLower();
					if (name.Contains("luyện tập") || name.Contains("luyen tap") || name.Contains("máy luyện") || name.Contains("tập luyện") || name.Contains("tự động"))
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
			if (!GameScr.canAutoPlay && !isAutoTrainBuffActive())
			{
				int bagIdx = findAutoTrainBagIndex();
				if (bagIdx >= 0)
				{
					Service.gI().useItem(0, 1, (sbyte)bagIdx, -1);
					notify("Tự bật Luyện Tập để dịch map!");
				}
				else
				{
					notify("Bật Tự Động Luyện Tập để dịch map mượt!");
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
		else if (dir == 1)
		{
			// Tìm vị trí có đất gần tâm map nhất (tránh rơi vào hố trống hoặc nước)
			for (int offset = 0; offset < TileMap.pxw / 4; offset += 24)
			{
				int testX1 = targetX + offset;
				if (testX1 < TileMap.pxw - 24 && TileMap.tileTypeAt(testX1, findGroundY(testX1), 2))
				{
					targetX = testX1;
					break;
				}
				if (offset > 0)
				{
					int testX2 = targetX - offset;
					if (testX2 > 24 && TileMap.tileTypeAt(testX2, findGroundY(testX2), 2))
					{
						targetX = testX2;
						break;
					}
				}
			}
		}

		int targetY = findGroundY(targetX);
		int dist = Res.distance(myChar.cx, myChar.cy, targetX, targetY);
		bool hasTdlt = isAutoTrainBuffActive() || isAutoTrain;

		myChar.currentMovePoint = null;
		Char.isLockKey = false;
		Char.ischangingMap = false;

		myChar.cx = targetX;
		myChar.cy = targetY;
		myChar.cxSend = targetX;
		myChar.cySend = targetY;
		myChar.statusMe = 1;
		myChar.cvy = 0;
		myChar.cvx = 0;
		myChar.delayFall = 0;

		Service.gI().charMove();
	}

	public static void handleQuickMapChange(int dir)
	{
		long now = mSystem.currentTimeMillis();
		if (now - lastChangeMapRequestTime < 250L) return;

		Char myChar = Char.myCharz();
		if (myChar == null || myChar.statusMe == 14 || myChar.cHP <= 0) return;

		if (Char.ischangingMap || Char.isLoadingMap || Controller.isStopReadMessage)
		{
			if (lastChangeMapRequestTime > 0L && now - lastChangeMapRequestTime > 4000L)
			{
				Char.ischangingMap = false;
				Char.isLockKey = false;
				InfoDlg.hide();
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
			// Tìm waypoint nằm ở khu vực giữa map (15% - 85% chiều rộng map)
			for (int i = 0; i < waypoints.Count; i++)
			{
				int wpX = (waypoints[i].minX + waypoints[i].maxX) / 2;
				if (wpX >= TileMap.pxw * 15 / 100 && wpX <= TileMap.pxw * 85 / 100)
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

	public static int GetPosWaypointX(Waypoint waypoint)
	{
		if (waypoint.maxX < 60)
			return 15;
		if (waypoint.minX > TileMap.pxw - 60)
			return TileMap.pxw - 15;
		return waypoint.minX + 30;
	}

	public static int GetPosWaypointY(Waypoint waypoint)
	{
		return waypoint.maxY;
	}

	public static void teleportToWaypoint(Waypoint wp, int dir = -1)
	{
		if (wp == null) return;
		Char myChar = Char.myCharz();
		if (myChar == null) return;

		int targetX = GetPosWaypointX(wp);
		int targetY = GetPosWaypointY(wp);
		myChar.cdir = (targetX > TileMap.pxw / 2) ? 1 : -1;

		myChar.currentMovePoint = null;
		Char.isLockKey = false;
		Char.ischangingMap = false;

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

		// Đặt tọa độ nhân vật
		myChar.cx = targetX;
		myChar.cy = targetY;
		myChar.cxSend = targetX;
		myChar.cySend = targetY;

		bool hasGround = TileMap.tileTypeAt(targetX, targetY + 12, 2);
		myChar.statusMe = hasGround ? 1 : 10;
		myChar.cvy = 0;
		myChar.cvx = 0;
		myChar.delayFall = 0;

		TileMap.SyncSpatialBuffer(targetX, targetY, (!isAutoTrainBuffActive()) ? 1 : 0);

		doRequestChangeMap(wp, targetX, targetY);

		string wpName = (wp.popup != null && wp.popup.says != null && wp.popup.says.Length > 0) ? wp.popup.says[0] : "Cổng";
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

		Char.isLockKey = false;
		Char.ischangingMap = false;

		sendChangeMapPacket(wp);
	}

	public static void sendChangeMapPacket(Waypoint wp)
	{
		if (wp == null) return;
		if (wp.isOffline || TileMap.isTrainingMap())
		{
			Service.gI().getMapOffline();
		}
		else if (wp.isEnter)
		{
			try { wp.perform(2, null); } catch { }
		}
		else
		{
			Service.gI().requestChangeMap();
		}
	}

	public static void sendDirectMovePacket(int x, int y)
	{
		TileMap.SyncSpatialBuffer(x, y, 0);
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
			if (now - lastTimeEatPean >= 1500L)
			{
				lastTimeEatPean = now;
				myChar.doUsePotion(); // Sử dụng đậu nếu có trong rương. Nếu hết đậu thì bỏ qua, tuyệt đối không gọi doUseHP() để tránh spam bong bóng chat
			}
		}

		// Kiểm tra khoảng cách an toàn giữa các lần tung skill để tránh xung đột hoạt ảnh hoặc spam mạng
		long minSkillInterval = (long)Mathf.Clamp(380f / Mathf.Max(1f, currentSpeed), 75f, 400f);
		if (now - lastSkillActionTime < minSkillInterval || now - lastAttackTime < minSkillInterval || isBusyAttacking(myChar, now))
		{
			return;
		}

		// 2. Dùng skill Trị thương (Cứu Namếc - ID 7) khi HP dưới 85% nếu người dùng CÓ TÍCH CHỌN
		if (selectedSkillIds.Contains(7) && myChar.cHP > 0 && myChar.cHP < myChar.cHPFull * 85 / 100)
		{
			for (int i = 0; i < myChar.vSkillFight.size(); i++)
			{
				Skill s = (Skill)myChar.vSkillFight.elementAt(i);
				if (s != null && s.template != null && s.template.id == 7)
				{
					if (now - s.lastTimeUseThisSkill >= s.coolDown && myChar.cMP >= getManaUse(s))
					{
						if (myChar.charFocus == null) myChar.charFocus = myChar;
						s.lastTimeUseThisSkill = now;
						lastAttackStartTime = now;
						lastAttackTime = now;
						lastSkillActionTime = now;
						GameScr.gI().doUseSkill(s, true);
						GameScr.isAutoPlay = false;
						return;
					}
				}
			}
		}

		// 3. Dùng skill Tái tạo năng lượng (Namếc - ID 8 hoặc 10) khi KI dưới 60% nếu người dùng CÓ TÍCH CHỌN
		if ((selectedSkillIds.Contains(8) || selectedSkillIds.Contains(10)) && myChar.cMP < myChar.cMPFull * 60 / 100)
		{
			for (int i = 0; i < myChar.vSkillFight.size(); i++)
			{
				Skill s = (Skill)myChar.vSkillFight.elementAt(i);
				if (s != null && s.template != null && (s.template.id == 8 || s.template.id == 10))
				{
					if (now - s.lastTimeUseThisSkill >= s.coolDown)
					{
						s.lastTimeUseThisSkill = now;
						lastAttackStartTime = now;
						lastAttackTime = now;
						lastSkillActionTime = now;
						GameScr.gI().doUseSkill(s, true);
						GameScr.isAutoPlay = false;
						return;
					}
				}
			}
		}

		// 4. ĐẺ TRỨNG NAMEC (ID 12): Tự động sinh đệ trứng mỗi khi hồi chiêu xong và đủ KI
		if (selectedSkillIds.Contains(12))
		{
			for (int i = 0; i < myChar.vSkillFight.size(); i++)
			{
				Skill s = (Skill)myChar.vSkillFight.elementAt(i);
				if (s != null && s.template != null && s.template.id == 12)
				{
					if (now - s.lastTimeUseThisSkill >= s.coolDown && myChar.cMP >= getManaUse(s))
					{
						s.lastTimeUseThisSkill = now;
						lastAttackStartTime = now;
						lastAttackTime = now;
						lastSkillActionTime = now;
						GameScr.gI().doUseSkillNotFocus(s);
						GameScr.isAutoPlay = false;
						return;
					}
				}
			}
		}

		// 5. BIẾN KHỈ XAYDA (ID 13): Tự động biến khỉ khi hết dạng khỉ và hồi chiêu xong
		if (selectedSkillIds.Contains(13))
		{
			if (myChar.isMonkey == 0 && !myChar.isWaitMonkey)
			{
				for (int i = 0; i < myChar.vSkillFight.size(); i++)
				{
					Skill s = (Skill)myChar.vSkillFight.elementAt(i);
					if (s != null && s.template != null && s.template.id == 13)
					{
						if (now - s.lastTimeUseThisSkill >= s.coolDown && myChar.cMP >= getManaUse(s))
						{
							s.lastTimeUseThisSkill = now;
							lastAttackStartTime = now;
							lastAttackTime = now;
							lastSkillActionTime = now;
							GameScr.gI().doUseSkillNotFocus(s);
							GameScr.isAutoPlay = false;
							return;
						}
					}
				}
			}
		}

		// 6. HUÝT SÁO (ID 21): Tự động huýt sáo tăng máu tối đa mỗi khi hồi chiêu
		if (selectedSkillIds.Contains(21))
		{
			for (int i = 0; i < myChar.vSkillFight.size(); i++)
			{
				Skill s = (Skill)myChar.vSkillFight.elementAt(i);
				if (s != null && s.template != null && s.template.id == 21)
				{
					if (now - s.lastTimeUseThisSkill >= s.coolDown && myChar.cMP >= getManaUse(s))
					{
						s.lastTimeUseThisSkill = now;
						lastAttackStartTime = now;
						lastAttackTime = now;
						lastSkillActionTime = now;
						GameScr.gI().doUseSkillNotFocus(s);
						GameScr.isAutoPlay = false;
						return;
					}
				}
			}
		}

		// 7. KHIÊN NĂNG LƯỢNG (ID 19): Tự động bật khiên khi hồi chiêu xong
		if (selectedSkillIds.Contains(19))
		{
			for (int i = 0; i < myChar.vSkillFight.size(); i++)
			{
				Skill s = (Skill)myChar.vSkillFight.elementAt(i);
				if (s != null && s.template != null && s.template.id == 19)
				{
					if (now - s.lastTimeUseThisSkill >= s.coolDown && myChar.cMP >= getManaUse(s))
					{
						s.lastTimeUseThisSkill = now;
						lastAttackStartTime = now;
						lastAttackTime = now;
						lastSkillActionTime = now;
						GameScr.gI().doUseSkillNotFocus(s);
						GameScr.isAutoPlay = false;
						return;
					}
				}
			}
		}

		// 8. THÁI DƯƠNG HẠ SAN (ID 6): Tự động choáng khi hồi chiêu xong
		if (selectedSkillIds.Contains(6))
		{
			for (int i = 0; i < myChar.vSkillFight.size(); i++)
			{
				Skill s = (Skill)myChar.vSkillFight.elementAt(i);
				if (s != null && s.template != null && s.template.id == 6)
				{
					if (now - s.lastTimeUseThisSkill >= s.coolDown && myChar.cMP >= getManaUse(s))
					{
						s.lastTimeUseThisSkill = now;
						lastAttackStartTime = now;
						lastAttackTime = now;
						lastSkillActionTime = now;
						GameScr.gI().doUseSkillNotFocus(s);
						GameScr.isAutoPlay = false;
						return;
					}
				}
			}
		}
	}

	// Kiểm tra quái có thể tấn công được không (hỗ trợ lọc quái theo vị trí)
	public static bool isMobTargetable(Mob mob)
	{
		if (mob == null || mob.isMobMe || mob.status == 0 || mob.status == 1 || mob.hp <= 0)
		{
			return false;
		}

		// Nếu người dùng bật "Chỉ pem quái theo vị trí chỉ định" và có chọn danh sách quái
		if (isFilterMob && selectedMobIds != null && selectedMobIds.Count > 0)
		{
			if (!selectedMobIds.Contains(mob.mobId))
			{
				return false;
			}
		}
		return true;
	}

	// Kiểm tra xem map đã hết quái cần pem chưa
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
			if (m != null && isMobTargetable(m))
			{
				return false; // Vẫn còn ít nhất 1 con quái cần pem còn sống
			}
		}

		return true; // Toàn bộ quái cần pem trong map đã chết -> Đổi khu!
	}

	// Logic Tự Động Đổi Khu Khi Hết Quái (Ưu tiên khu có trong map, ít người nhất, không mở panel)
	private static void updateAutoChangeZone(long now)
	{
		// Nếu người chơi đang bận trong UI hệ thống (nói chuyện NPC, chat): Tạm dừng
		if (isPlayerBusyInUI())
		{
			return;
		}

		// Nhận diện khi đã chuyển sang khu mới thành công (zoneID thay đổi)
		if (TileMap.zoneID != currentZoneID)
		{
			currentZoneID = TileMap.zoneID;
			isWaitingZoneData = false;
			isSilentZoneRequest = false;
			lastZoneChangeTime = now;
			lastZoneAttemptTime = now;
			Char.myCharz().mobFocus = null;
			return;
		}

		// Nếu trong map vẫn còn quái sống -> không cần đổi khu
		if (!isMapOutOfMobs())
		{
			isWaitingZoneData = false;
			isSilentZoneRequest = false;
			return;
		}

		// ĐÃ HẾT QUÁI TRONG MAP!
		// Giãn cách tối thiểu sau khi vừa đổi khu thành công (5 giây cooldown chuẩn server)
		if (now - lastZoneChangeTime < 5000L)
		{
			return;
		}

		// Nếu đang chờ nhận danh sách khu từ server
		if (isWaitingZoneData)
		{
			if (now - lastZoneAttemptTime < 2500L)
			{
				return;
			}
			isWaitingZoneData = false;
		}

		// Nếu vừa gửi lệnh đổi khu trước đó -> chờ tối thiểu 3000ms để server xử lý
		if (now - lastZoneAttemptTime < 3000L)
		{
			return;
		}

		// Gửi yêu cầu lấy danh sách khu thực tế của map từ server (chạy ngầm, không mở panel)
		// để lấy chính xác các khu tồn tại và số lượng người chơi trong mỗi khu
		isWaitingZoneData = true;
		isSilentZoneRequest = true;
		lastZoneAttemptTime = now;
		Service.gI().openUIZone();
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
	// Tự động nhặt vật phẩm & Bùa thu hút (Sử dụng tọa độ chuẩn, hỗ trợ Teleport chớp nhoáng khi bật Nhặt Toàn Map)
	private static void updateAutoPick(long now)
	{
		try
		{
			if (now < pauseAttackUntil || isPlayerBusyInUI() || Char.ischangingMap || Controller.isStopReadMessage)
			{
				return;
			}

			Char myChar = Char.myCharz();
			if (myChar == null || myChar.statusMe == 14 || TileMap.mapID == -1)
			{
				return;
			}

			// Chỉ nhặt khi Tự Động Nhặt đang BẬT hoặc có Bùa Thu Hút
			if (!isAutoPick && !isBuaThuHut)
			{
				return;
			}

			if (GameScr.vItemMap == null || GameScr.vItemMap.size() == 0)
			{
				return;
			}

			if (now - lastPickItemTime < 250L)
			{
				return;
			}

			List<int> pickIds = parseFilterIds(pickItemFilter);
			List<int> excludeIds = parseFilterIds(excludeItemFilter);

			int pickCount = 0;
			int total = GameScr.vItemMap.size();
			for (int i = 0; i < total; i++)
			{
				if (i >= GameScr.vItemMap.size() || pickCount >= 3)
				{
					break;
				}
				ItemMap item = (ItemMap)GameScr.vItemMap.elementAt(i);
				if (item == null || item.template == null)
				{
					continue;
				}

				// 1. Phân quyền nhặt: Nếu KHÔNG bật "Nhặt vật phẩm của người khác" -> chỉ nhặt đồ của mình hoặc rơi tự do
				if (!isPickOthers)
				{
					if (item.playerId > 0 && item.playerId != myChar.charID)
					{
						continue;
					}
				}

				// 2. Danh sách Loại Trừ (excludeItemFilter): Tuyệt đối KHÔNG nhặt các ID này
				if (excludeIds.Count > 0 && excludeIds.Contains(item.template.id))
				{
					continue;
				}

				// 3. Danh sách Nhặt theo ID (pickItemFilter): Nếu có nhập ID -> CHỈ NHẶT các ID có trong danh sách
				if (pickIds.Count > 0 && !pickIds.Contains(item.template.id))
				{
					continue;
				}

				// 4. Khoảng cách và hình thức nhặt:
				int itemRealX = (item.xEnd != 0) ? item.xEnd : item.x;
				int itemRealY = (item.yEnd != 0) ? item.yEnd : item.y;
				int distToChar = Res.distance(itemRealX, itemRealY, myChar.cx, myChar.cy);

				// Trường hợp 1: Ở gần (phạm vi 75px) hoặc đang dùng Bùa Thu Hút -> Nhặt trực tiếp
				if (distToChar <= 75 || isBuaThuHut)
				{
					Service.gI().pickItem(item.itemMapID);
					pickCount++;
					lastPickItemTime = now;
				}
				// Trường hợp 2: Ở xa và người dùng CÓ TÍCH CHỌN "Nhặt Toàn Map" -> Dịch chuyển tới sát item để nhặt
				else if (isPickAllMap && !isPlayerManuallyMoving())
				{
					myChar.cx = itemRealX;
					myChar.cy = itemRealY;
					myChar.currentMovePoint = null;
					myChar.cvx = (myChar.cvy = 0);
					myChar.delayFall = 0;
					bool hasGround = TileMap.tileTypeAt(itemRealX, itemRealY + 12, 2);
					myChar.statusMe = hasGround ? 1 : 10;
					Service.gI().charMove();
					Service.gI().pickItem(item.itemMapID);
					pickCount++;
					lastPickItemTime = now;
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

			// Không vứt khi đang nhập văn bản hoặc khi đang chuyển map / nhân vật chết
			if (ChatTextField.isShowing || (GameCanvas.inputDlg != null && GameCanvas.inputDlg.tfInput != null && GameCanvas.inputDlg.tfInput.isFocus))
			{
				return;
			}

			Char myChar = Char.myCharz();
			if (myChar == null || myChar.statusMe == 14 || Char.ischangingMap || TileMap.mapID == -1 || myChar.arrItemBag == null)
			{
				return;
			}

			// 1. Xử lý yêu cầu xác nhận vứt đang đợi (đã nhận form từ server và chờ đúng độ trễ)
			if (pendingDropItemObject != null)
			{
				// Nếu đã đến thời gian được phép gửi xác nhận (sau 400ms bình thường hoặc sau 2.5s nếu bị "Từ từ đừng vội")
				if (now >= pendingDropConfirmTime)
				{
					ItemObject itemObj = pendingDropItemObject;
					pendingDropItemObject = null;
					isWaitingDropConfirm = true;
					lastDropItemTime = now;
					GameCanvas.panel.perform(2004, itemObj);
					GameCanvas.endDlg();
				}
				return;
			}

			// 2. Nếu server đang yêu cầu hoãn ("Từ từ đừng vội") hoặc mã bảo vệ
			if (now < dropRateLimitUntil || now < securityCodeLockedUntil)
			{
				return;
			}

			// 3. Quản lý timeout khi đang đợi server phản hồi vứt
			if (isWaitingDropConfirm)
			{
				if (now - lastDropItemTime > 2500L)
				{
					// Quá 2.5s không thấy server phản hồi hoàn tất -> đếm lỗi và reset để thử lại hoặc chuyển món
					isWaitingDropConfirm = false;
					isAutoDropAction = false;
					if (currentDropTemplateId != -1)
					{
						recordDropFail(currentDropTemplateId);
					}
				}
				else
				{
					return;
				}
			}

			// 4. Giãn cách an toàn tối thiểu 500ms giữa 2 lần vứt
			if (now - lastDropItemTime < 500L)
			{
				return;
			}

			List<int> dropIds = parseFilterIds(dropItemFilter);
			if (dropIds.Count == 0)
			{
				return;
			}

			// 5. Quét tìm món đầu tiên trong hành trang trùng danh sách vứt
			sbyte targetBagIdx = -1;
			int targetTemplateId = -1;

			for (int i = 0; i < myChar.arrItemBag.Length; i++)
			{
				Item item = myChar.arrItemBag[i];
				if (item != null && item.template != null && dropIds.Contains(item.template.id))
				{
					int tId = item.template.id;
					if (dropSkipUntil.ContainsKey(tId) && now < dropSkipUntil[tId])
					{
						continue; // Đang tạm bỏ qua món này vì server không cho vứt
					}
					targetBagIdx = (sbyte)i;
					targetTemplateId = tId;
					break; // Vứt lần lượt từng món tìm thấy!
				}
			}

			if (targetBagIdx == -1)
			{
				return;
			}

			// 6. Gửi yêu cầu vứt vật phẩm này lên server
			currentDropBagIdx = targetBagIdx;
			currentDropTemplateId = targetTemplateId;
			isAutoDropAction = true;
			isManualDropAction = false;
			isWaitingDropConfirm = true;
			lastDropItemTime = now;

			Service.gI().useItem(1, 1, targetBagIdx, -1);
		}
		catch (Exception)
		{
		}
	}

	public static bool isItemCannotBeDropped(Item item, out string reason)
	{
		reason = "";
		return false;
	}

	// Logic Tàn Sát (Có cơ chế KHÓA MỤC TIÊU - không bị loạn quái)
	private static void updateTanSat(long now)
	{
		// Nếu đang ưu tiên mở Capsule / Hợp thể Porata -> Tạm ngưng đánh để server xử lý lệnh
		if (now < pauseAttackUntil)
		{
			return;
		}

		// Nếu người chơi đang bận trong UI (menu, panel, chat, nói chuyện NPC) hoặc đang đổi map: KHÔNG can thiệp!
		if (isPlayerBusyInUI() || Char.ischangingMap || Controller.isStopReadMessage)
		{
			return;
		}

		// Nếu đang khóa người chơi khác (Phím I), không tự ý nhắm hoặc tấn công quái
		if (lockedTargetCharId != -1)
		{
			return;
		}

		Char myChar = Char.myCharz();
		if (myChar.statusMe == 14)
		{
			return;
		}

		myChar.delayFall = 0;

		// KHÓA MỤC TIÊU: Nếu con quái đang nhắm vẫn còn sống và hợp lệ -> giữ nguyên quái đó!
		Mob targetMob = myChar.mobFocus;
		if (targetMob == null || !isMobTargetable(targetMob))
		{
			targetMob = null;
			myChar.mobFocus = null;

			// Tìm quái mới gần nhất còn sống và thỏa mãn bộ lọc quái
			int minDistance = int.MaxValue;
			if (GameScr.vMob != null)
			{
				for (int i = 0; i < GameScr.vMob.size(); i++)
				{
					Mob mob = (Mob)GameScr.vMob.elementAt(i);
					if (mob != null && isMobTargetable(mob))
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

		// Nếu có quái sống -> Tấn công quái
		if (targetMob != null)
		{
			myChar.mobFocus = targetMob;
			myChar.charFocus = null;
			myChar.itemFocus = null;
			myChar.npcFocus = null;

			// NẾU BẬT "ĐỨNG IM ĐÁNH QUÁI TOÀN MAP": ĐỨNG IM 1 CHỖ TRONG MAP ĐÁNH TẤT CẢ QUÁI, KHÔNG DỊCH CHUYỂN
			if (isStandStill)
			{
				if (!isPlayerManuallyMoving())
				{
					myChar.cdir = (myChar.cx < targetMob.x) ? 1 : -1;
				}

				long attackDelay = (long)Mathf.Clamp(380f / Mathf.Max(1f, currentSpeed), 75f, 400f);
				if (now - lastAttackTime >= attackDelay)
				{
					attackTarget(targetMob);
				}
			}
			else
			{
				// KHI TẮT "ĐỨNG IM": DỊCH CHUYỂN TỨC THỜI TỚI SÁT QUÁI ĐỂ ĐÁNH
				int offset = (myChar.cx <= targetMob.x) ? -25 : 25;
				int desiredX = targetMob.x + offset;
				int desiredY = targetMob.y;

				if (desiredX < 24) desiredX = 24;
				if (desiredX > TileMap.pxw - 24) desiredX = TileMap.pxw - 24;

				if (!isPlayerManuallyMoving())
				{
					if (Res.abs(myChar.cx - desiredX) > 20 || Res.abs(myChar.cy - desiredY) > 20)
					{
						myChar.cx = desiredX;
						myChar.cy = desiredY;
						myChar.cdir = (myChar.cx < targetMob.x) ? 1 : -1;
						myChar.currentMovePoint = null;
						myChar.cvx = (myChar.cvy = 0);
						myChar.delayFall = 0;
						bool hasGround = TileMap.tileTypeAt(desiredX, desiredY + 12, 2);
						myChar.statusMe = hasGround ? 1 : 10;

						if (now - lastMoveSendTime >= 150L)
						{
							lastMoveSendTime = now;
							Service.gI().charMove();
						}
					}
					else
					{
						myChar.cdir = (myChar.cx < targetMob.x) ? 1 : -1;
						myChar.currentMovePoint = null;
						myChar.cvx = (myChar.cvy = 0);
						if (myChar.statusMe == 2)
						{
							myChar.statusMe = ((TileMap.tileTypeAtPixel(myChar.cx, myChar.cy) & 2) == 2) ? 1 : 6;
						}
					}
				}
				else
				{
					myChar.cdir = (myChar.cx < targetMob.x) ? 1 : -1;
				}

				// Chỉ tung đòn khi đã ở sát quái (khoảng cách <= 45px)
				if (Res.abs(myChar.cx - targetMob.x) <= 45 && Res.abs(myChar.cy - targetMob.y) <= 45)
				{
					long attackDelay = (long)Mathf.Clamp(380f / Mathf.Max(1f, currentSpeed), 75f, 400f);
					if (now - lastAttackTime >= attackDelay)
					{
						attackTarget(targetMob);
					}
				}
			}
		}
	}

	// Logic Tự Luyện Tập độc lập (Đứng đâu pem đó, không dịch chuyển)
	private static void updateAutoTrain(long now)
	{
		// Nếu đang ưu tiên mở Capsule / Hợp thể Porata -> Tạm ngưng đánh để server xử lý lệnh
		if (now < pauseAttackUntil)
		{
			return;
		}

		if (isPlayerBusyInUI())
		{
			return;
		}
		Char myChar = Char.myCharz();
		if (myChar.statusMe == 14)
		{
			return;
		}

		if (!isStandStill && !isPlayerManuallyMoving())
		{
			myChar.delayFall = 0;
			myChar.currentMovePoint = null;
			myChar.cvx = (myChar.cvy = 0);
			if (myChar.statusMe == 2)
			{
				myChar.statusMe = ((TileMap.tileTypeAtPixel(myChar.cx, myChar.cy) & 2) == 2) ? 1 : 6;
			}
		}
		Mob mob = myChar.mobFocus;

		// Nếu chưa chọn quái, tìm quái gần nhất trong bán kính 200px (hoặc toàn map nếu bật đứng im)
		if (mob == null || !isMobTargetable(mob))
		{
			mob = null;
			myChar.mobFocus = null;
			int minDistance = isStandStill ? int.MaxValue : 200;
			if (GameScr.vMob != null)
			{
				for (int i = 0; i < GameScr.vMob.size(); i++)
				{
					Mob m = (Mob)GameScr.vMob.elementAt(i);
					if (m != null && isMobTargetable(m))
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
			myChar.charFocus = null;
			myChar.itemFocus = null;
			myChar.npcFocus = null;
			long attackDelay = 380L;
			if (now - lastAttackTime >= attackDelay)
			{
				attackTarget(mob);
			}
		}
	}

	private static long lastAttackStartTime = 0L;
	private static long lastSkillActionTime = 0L;

	// Kiểm tra nhân vật có đang bận thực hiện đòn đánh/chưởng/gồng chiêu hay không
	private static bool isBusyAttacking(Char myChar, long now)
	{
		if (myChar == null)
		{
			return false;
		}

		// Watchdog phòng kẹt: Tự động điều chỉnh theo tốc độ game hiện tại
		long watchdogTimeout = (long)Mathf.Clamp(1200f / Mathf.Max(1f, currentSpeed), 300f, 1200f);
		if (lastAttackStartTime > 0L && now - lastAttackStartTime > watchdogTimeout)
		{
			myChar.dart = null;
			myChar.arr = null;
			myChar.skillPaint = null;
			myChar.isCharge = false;
			myChar.isFlyAndCharge = false;
			myChar.isWaitMonkey = false;
			lastAttackStartTime = 0L;
			return false;
		}

		// 1. Đang có phi đạn / chưởng bay tới mục tiêu (Kamejoko, Masenko, Antomic, v.v.)
		if (myChar.dart != null || myChar.arr != null)
		{
			return true;
		}

		// 2. Đang trong frame hoạt ảnh đánh/chưởng/buff chưa tới frame kết thúc
		if (myChar.skillPaint != null)
		{
			if (myChar.skillInfoPaint() == null || myChar.indexSkill < myChar.skillInfoPaint().Length)
			{
				return true;
			}
		}

		// 3. Đang trong trạng thái gồng chiêu (Quả cầu kênh khi, Makankosappo, Tái tạo năng lượng, v.v.)
		if (myChar.isCharge || myChar.isFlyAndCharge || myChar.isUseChargeSkill() || myChar.isWaitMonkey)
		{
			return true;
		}

		return false;
	}

	// Đánh quái: Thực thi xoay tua skill mượt mà theo bảng cài đặt, chống ngắt chiêu, chống trượt dame và lag game
	private static void attackTarget(Mob mob)
	{
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.vSkillFight == null || myChar.vSkillFight.size() == 0 || mob == null)
		{
			return;
		}

		long now = mSystem.currentTimeMillis();
		long minSkillInterval = (long)Mathf.Clamp(380f / Mathf.Max(1f, currentSpeed), 75f, 400f);

		// Ngăn chặn tung skill trùng lặp / cùng thời điểm gây lag dame hoặc đơ skill
		if (now - lastSkillActionTime < minSkillInterval || now - lastAttackTime < minSkillInterval || isBusyAttacking(myChar, now))
		{
			return;
		}

		Skill skillToUse = null;

		// 1. NẾU NGƯỜI DÙNG ĐÃ TÍCH CHỌN SKILL TRONG BẢNG:
		if (selectedSkillIds.Count > 0)
		{
			Skill readySpecialSkill = null;
			Skill selectedBasicSkill = null;

			for (int i = 0; i < myChar.vSkillFight.size(); i++)
			{
				Skill s = (Skill)myChar.vSkillFight.elementAt(i);
				if (s == null || s.template == null || !selectedSkillIds.Contains(s.template.id))
				{
					continue;
				}

				// Bỏ qua các skill buff/hồi máu/triệu hồi (đã xử lý độc lập trong checkAutoBuffOrHeal)
				if (s.template.isBuffToPlayer() || s.template.isUseAlone() || 
				    s.template.id == 6 || s.template.id == 7 || s.template.id == 8 || 
				    s.template.id == 10 || s.template.id == 12 || s.template.id == 13 || 
				    s.template.id == 19 || s.template.id == 21)
				{
					continue;
				}

				// Chiêu đấm cơ bản (không tốn KI và không có hồi chiêu)
				if (s.coolDown == 0)
				{
					if (selectedBasicSkill == null)
					{
						selectedBasicSkill = s;
					}
					continue;
				}

				// Với chiêu có hồi chiêu:
				bool isOffCooldown = (now - s.lastTimeUseThisSkill >= s.coolDown);
				bool hasEnoughKI = (myChar.cMP >= getManaUse(s));

				if (isOffCooldown && hasEnoughKI)
				{
					if (readySpecialSkill == null || s.coolDown > readySpecialSkill.coolDown)
					{
						readySpecialSkill = s;
					}
				}
			}

			// Ưu tiên 1: Chiêu đặc biệt đã hồi xong và đủ KI
			if (readySpecialSkill != null)
			{
				skillToUse = readySpecialSkill;
			}
			// Ưu tiên 2: Nếu chiêu đặc biệt chưa đủ KI hoặc đang hồi, dùng chiêu đấm cơ bản (nếu người dùng có tích chọn trong bảng)
			else if (selectedBasicSkill != null)
			{
				skillToUse = selectedBasicSkill;
			}
			// Ưu tiên 3: Nếu chiêu đặc biệt đang hồi hoặc chưa đủ KI, dùng đòn đấm cơ bản mặc định để tiếp tục đánh
			else if (myChar.vSkillFight.size() > 0)
			{
				Skill s0 = (Skill)myChar.vSkillFight.elementAt(0);
				if (s0 != null && s0.template != null && !s0.template.isBuffToPlayer() && !s0.template.isUseAlone())
				{
					skillToUse = s0;
				}
			}
		}
		// 2. NẾU KHÔNG TÍCH CHỌN SKILL NÀO (Bảng trống):
		// Mặc định chỉ dùng đòn đấm cơ bản (index 0)
		else if (myChar.vSkillFight.size() > 0)
		{
			Skill s0 = (Skill)myChar.vSkillFight.elementAt(0);
			if (s0 != null && s0.template != null && !s0.template.isBuffToPlayer() && !s0.template.isUseAlone())
			{
				skillToUse = s0;
			}
		}

		if (skillToUse != null)
		{
			// Khóa chặt mục tiêu vào quái
			myChar.mobFocus = mob;
			myChar.charFocus = null;
			myChar.itemFocus = null;

			if (!isPlayerManuallyMoving())
			{
				myChar.cdir = (myChar.cx < mob.x) ? 1 : -1;
			}

			// NẾU ĐANG BẬT "ĐỨNG IM ĐÁNH" (TÀN SÁT KHÔNG DỊCH CHUYỂN):
			// Người chơi vẫn di chuyển tự do 100%, không bị hủy chuyển động, không bị khựng cử chỉ đánh
			if (isStandStill)
			{
				if (myChar.myskill == null || myChar.myskill.template.id != skillToUse.template.id)
				{
					Service.gI().selectSkill(skillToUse.template.id);
					myChar.myskill = skillToUse;
				}

				lastAttackStartTime = now;
				lastAttackTime = now;
				lastSkillActionTime = now;
				skillToUse.lastTimeUseThisSkill = now;

				long mana = getManaUse(skillToUse);
				if (mana > 0 && myChar.cMP >= mana)
				{
					myChar.cMP -= (int)mana;
				}

				if (skillToUse.template.isUseAlone())
				{
					myChar.useSkillNotFocus();
				}
				else if (myChar.isUseChargeSkill())
				{
					myChar.sendUseChargeSkill();
				}
				else
				{
					MyVector vMob = new MyVector();
					vMob.addElement(mob);
					Service.gI().sendPlayerAttack(vMob, new MyVector(), 1);
				}

				GameScr.isAutoPlay = false;
				return;
			}

			// Khi TẮT "Đứng im" (Tàn sát bình thường có dịch chuyển):
			if (!isPlayerManuallyMoving())
			{
				myChar.currentMovePoint = null;
				myChar.cvx = (myChar.cvy = 0);
				if (myChar.statusMe == 2)
				{
					myChar.statusMe = ((TileMap.tileTypeAtPixel(myChar.cx, myChar.cy) & 2) == 2) ? 1 : 6;
				}
			}

			// Chỉ gửi packet selectSkill lên Server nếu skill thực sự thay đổi (tiết kiệm băng thông, chống giật lag mạng)
			if (myChar.myskill == null || myChar.myskill.template.id != skillToUse.template.id)
			{
				Service.gI().selectSkill(skillToUse.template.id);
			}

			myChar.myskill = skillToUse;
			lastAttackStartTime = now;
			lastAttackTime = now;
			lastSkillActionTime = now;
			skillToUse.lastTimeUseThisSkill = now;

			// Tung chiêu theo đúng loại skill chuẩn của game
			if (skillToUse.template.isUseAlone())
			{
				myChar.useSkillNotFocus();
			}
			else if (myChar.isUseChargeSkill())
			{
				myChar.sendUseChargeSkill();
			}
			else
			{
				bool flag = TileMap.tileTypeAt(myChar.cx, myChar.cy, 2);
				myChar.setSkillPaint(GameScr.sks[skillToUse.skillId], (!flag) ? 1 : 0);
				if (flag)
				{
					myChar.delayFall = 20;
				}
				myChar.currentFireByShortcut = true;
			}

			GameScr.isAutoPlay = false;
		}
	}

	// Callback khi nhận danh sách khu từ Server
	public static void onReceiveZones()
	{
		if (isAutoChangeZone && isWaitingZoneData)
		{
			isWaitingZoneData = false;
			isSilentZoneRequest = false;
			lastZoneChangeTime = mSystem.currentTimeMillis();
			lastZoneAttemptTime = lastZoneChangeTime;

			int[] zones = GameScr.gI().zones;
			int[] numPlayer = GameScr.gI().numPlayer;
			int[] maxPlayer = GameScr.gI().maxPlayer;

			if (zones != null && zones.Length > 0)
			{
				int bestZone = -1;
				int minPlayerCount = int.MaxValue;

				// Tìm vị trí của khu hiện tại trong mảng zones
				int curIndex = 0;
				for (int i = 0; i < zones.Length; i++)
				{
					if (zones[i] == TileMap.zoneID)
					{
						curIndex = i;
						break;
					}
				}

				// Quét xoay vòng từ khu kế tiếp để ưu tiên khu ít người nhất và luân phiên đều đặn
				for (int offset = 1; offset < zones.Length; offset++)
				{
					int i = (curIndex + offset) % zones.Length;
					int zId = zones[i];
					if (zId == -1 || zId == TileMap.zoneID) continue;

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

				// Nếu tất cả các khu khác đều đầy, chọn khu kế tiếp theo danh sách
				if (bestZone == -1)
				{
					int nextIdx = (curIndex + 1) % zones.Length;
					bestZone = zones[nextIdx];
				}

				notify("Hết quái -> Chuyển sang khu " + bestZone + " (" + (minPlayerCount == int.MaxValue ? 0 : minPlayerCount) + " người)");
				Service.gI().requestChangeZone(bestZone, -1);
			}

			if (GameCanvas.panel != null && GameCanvas.panel.isShow && GameCanvas.panel.type == 3)
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

	// Kiểm tra xem Bùa / Buff Tự Động Luyện Tập có đang hoạt động hay không
	public static bool isAutoTrainBuffActive()
	{
		try
		{
			if (GameScr.canAutoPlay)
			{
				return true;
			}
			// 1. Quét Char.vItemTime (Icon bánh răng vàng đặc trưng của TDLT)
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
			// 2. Quét GameScr.textTime nếu có
			if (GameScr.textTime != null)
			{
				for (int j = 0; j < GameScr.textTime.size(); j++)
				{
					ItemTime t2 = (ItemTime)GameScr.textTime.elementAt(j);
					if (t2 != null && !string.IsNullOrEmpty(t2.text))
					{
						string s = t2.text.ToLower();
						if (s.Contains("luyện tập") || s.Contains("luyen tap") || s.Contains("tự động"))
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
				if (it.template.id == 521 || it.template.iconID == 4387 || it.template.iconID == 4028 || it.template.iconID == 521 || it.template.iconID == 4330 || it.template.id == 721)
				{
					return it;
				}
				if (!string.IsNullOrEmpty(it.template.name))
				{
					string name = it.template.name.ToLower();
					if (name.Contains("luyện tập") || name.Contains("luyen tap") || name.Contains("tập luyện") || name.Contains("máy luyện") || name.Contains("tự động"))
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

		// Hiển thị trạng thái khóa mục tiêu (Phím I) nếu đang khóa: chữ trắng bóng đen nổi bật, không có khung nền che buff
		if (lockedTargetCharId != -1)
		{
			string lockText = "🔒 [Đang khóa: " + lockedTargetCharName + "]";
			int lockX = (GameCanvas.w > 260) ? 140 : 6;
			int lockY = (GameCanvas.w > 260) ? 8 : 42;
			mFont.tahoma_7b_dark.drawString(g, lockText, lockX + 1, lockY + 1, 0);
			mFont.tahoma_7b_white.drawString(g, lockText, lockX, lockY, 0);
		}

		// Hiển thị trạng thái Auto Mở Vật Phẩm
		if (isAutoOpeningItem)
		{
			string autoOpenStatus = (autoOpenMode == 0)
				? ("⚡ [Auto Mở: " + autoOpenItemName + " (" + autoOpenRemainingCount + "/" + autoOpenTargetCount + ")]")
				: ("⚡ [Auto Mở: " + autoOpenItemName + " (Đã mở: " + autoOpenCountOpened + ")]");
			int statusX = GameCanvas.w / 2;
			int statusY = 5;
			mFont.tahoma_7b_dark.drawString(g, autoOpenStatus, statusX + 1, statusY + 1, mFont.CENTER);
			mFont.tahoma_7b_green2.drawString(g, autoOpenStatus, statusX, statusY, mFont.CENTER);
		}

		// Hiển thị thông tin Map, Khu, Toạ độ và Thời gian như hình ảnh mẫu yêu cầu
		Char curChar = Char.myCharz();
		if (curChar != null && GameCanvas.currentScreen is GameScr)
		{
			string mapName = TileMap.mapName;
			if (string.IsNullOrEmpty(mapName) || mapName == "0") mapName = XmapManager.getMapName(TileMap.mapID);
			string lineMap = "Map: " + mapName + " [" + TileMap.mapID + "-" + TileMap.zoneID + "-" + curChar.cx + "-" + curChar.cy + "]";
			string lineTime = "Time: " + DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");

			int menuX = (GameScr.gI() != null && GameScr.gI().cmdMenu != null) ? GameScr.gI().cmdMenu.x : 0;
			int infoX = menuX + 21;
			int infoY = (Char.vItemTime != null && Char.vItemTime.size() > 0) ? 88 : 50;
			if (GameScr.textTime != null && GameScr.textTime.size() > 0)
			{
				infoY += GameScr.textTime.size() * 12;
			}

			mFont.tahoma_7_white.drawString(g, lineMap, infoX, infoY, 0, mFont.tahoma_7b_dark);
			mFont.tahoma_7_white.drawString(g, lineTime, infoX, infoY + 11, 0, mFont.tahoma_7b_dark);

			BossManager.Paint(g);
		}


		// Nếu người dùng chủ động tích chọn "Hiện bảng HUD màn hình" thì mới vẽ thêm khung chữ
		if (isShowHUD)
		{
			int x = 6;
			int y = 45;
			int lineH = 11;

			g.setColor(0, 0.45f);
			g.fillRect(x - 3, y - 3, 140, 94);

			mFont font = mFont.tahoma_7_white;
			mFont fontYellow = mFont.tahoma_7_yellow;

			fontYellow.drawString(g, "[MOD NRO - MENU (X)]", x, y, 0);
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

		// 6. BẢNG CHỌN QUÁI THEO VỊ TRÍ (HÌNH ẢNH MẪU YÊU CẦU)
		if (isShowMobDialog)
		{
			paintMobDialog(g);
		}

		// 7. BẢNG CÀI ĐẶT GOBACK RIÊNG BIỆT (THEO YÊU CẦU)
		if (isShowGobackDialog)
		{
			paintGobackDialog(g);
		}

		// 8. BẢNG CHỌN MAP DỊCH CHUYỂN (XMAP)
		if (XmapManager.isShowXmapDialog)
		{
			XmapManager.paintXmapDialog(g);
		}
	}

	// =============================================================
	// 1. VẼ BẢNG CÀI ĐẶT CHÍNH (MAIN CONTROL PANEL) - THIẾT KẾ 3 CỤM/TAB GỌN GÀNG, THẨM MỸ
	// =============================================================
	private static void paintMainControlDialog(mGraphics g)
	{
		int boxW = 356;
		int boxH = 224;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;
		int headerH = 24;

		// 1. Nền vàng cát & viền đậm
		g.setColor(14860437);
		g.fillRect(boxX, boxY, boxW, boxH);
		g.setColor(2758924);
		g.drawRect(boxX, boxY, boxW, boxH);

		// 2. Tiêu đề xanh chính: BẢNG CÀI ĐẶT MOD (X)
		g.setColor(1800936);
		g.fillRect(boxX, boxY, boxW, headerH);
		mFont.tahoma_7b_white.drawString(g, "BẢNG CÀI ĐẶT MOD (X)", boxX + boxW / 2, boxY + 5, mFont.CENTER);

		// Nút [X] đóng ở góc trên bên phải
		int btnCloseX = boxX + boxW - 20;
		int btnCloseY = boxY + 4;
		g.setColor(13382451);
		g.fillRect(btnCloseX, btnCloseY, 16, 16);
		g.setColor(16777215);
		g.drawRect(btnCloseX, btnCloseY, 16, 16);
		mFont.tahoma_7b_white.drawString(g, "X", btnCloseX + 8, btnCloseY + 2, mFont.CENTER);

		// 3. THANH 8 TAB CHỨC NĂNG (Chiến Đấu | Săn Boss | Trong Khu | Tự Nhặt | Auto Item | Đồ Hoạ | FPS | Hệ Thống)
		int tabY = boxY + headerH + 3;
		int tabH = 21;
		int tabW = (boxW - 14) / 8; // ~42px mỗi tab
		string[] tabNames = new string[] { "Chiến Đấu", "Săn Boss", "Trong Khu", "Tự Nhặt", "Auto Item", "Đồ Hoạ", "FPS", "Hệ Thống" };

		for (int t = 0; t < 8; t++)
		{
			int curTabX = boxX + 7 + t * tabW;
			bool isSelectedTab = (activeMainDialogTab == t);

			if (isSelectedTab)
			{
				g.setColor(1800936); // Xanh dương đậm nổi bật
				g.fillRect(curTabX, tabY, tabW, tabH);
				g.setColor(16777215); // Viền trắng
				g.drawRect(curTabX, tabY, tabW, tabH);
				mFont.tahoma_7b_white.drawString(g, tabNames[t], curTabX + tabW / 2, tabY + 4, mFont.CENTER);
			}
			else
			{
				g.setColor(12693910); // Xám bạc
				g.fillRect(curTabX, tabY, tabW, tabH);
				g.setColor(5985856);
				g.drawRect(curTabX, tabY, tabW, tabH);
				mFont.tahoma_7b_dark.drawString(g, tabNames[t], curTabX + tabW / 2, tabY + 4, mFont.CENTER);
			}
		}

		// Khung chứa nội dung tab (Content Area)
		int contentY = tabY + tabH + 4;
		int contentH = 136;
		g.setColor(15720130); // Nền sáng
		g.fillRect(boxX + 6, contentY, boxW - 12, contentH);
		g.setColor(2758924);
		g.drawRect(boxX + 6, contentY, boxW - 12, contentH);

		// -------------------------------------------------------------
		// TAB 0: ⚔ CHIẾN ĐẤU
		// -------------------------------------------------------------
		if (activeMainDialogTab == 0)
		{
			int rowH = 17;
			int startY = contentY + 4;

			drawCheckboxRow(g, boxX + 16, startY, "Tàn sát quái trong map (ts)", isTanSat);
			drawCheckboxRow(g, boxX + 16, startY + rowH, "Đứng im pem quái toàn map (K di chuyển)", isStandStill);
			drawCheckboxRow(g, boxX + 16, startY + rowH * 2, "Chỉ pem quái theo vị trí chỉ định", isFilterMob);
			drawCheckboxRow(g, boxX + 16, startY + rowH * 3, "Tự động đổi khu khi hết quái (ck)", isAutoChangeZone);
			drawCheckboxRow(g, boxX + 16, startY + rowH * 4, "Tự động luyện tập TDLT (Phím T)", isAutoTrain || isAutoTrainBuffActive());

			// 4 Nút cài đặt: [ 🎯 Quái Pem ] & [ ⚡ Cài Skill ] & [ 🔄 Goback ] & [ 🗺️ Xmap ]
			int btnActionY = startY + rowH * 5 + 8;
			int gap = 6;
			int btnW = (boxW - 32 - gap * 3) / 4; // ~72px
			int btnH = 22;

			// Nút 1: Chọn Quái
			int btnMobX = boxX + 16;
			g.setColor(1800936);
			g.fillRect(btnMobX, btnActionY, btnW, btnH);
			g.setColor(2758924);
			g.drawRect(btnMobX, btnActionY, btnW, btnH);
			mFont.tahoma_7b_white.drawString(g, "Quái Pem", btnMobX + btnW / 2, btnActionY + 4, mFont.CENTER);

			// Nút 2: Cài Skill
			int btnSkillX = btnMobX + btnW + gap;
			g.setColor(1800936);
			g.fillRect(btnSkillX, btnActionY, btnW, btnH);
			g.setColor(2758924);
			g.drawRect(btnSkillX, btnActionY, btnW, btnH);
			mFont.tahoma_7b_white.drawString(g, "Cài Skill", btnSkillX + btnW / 2, btnActionY + 4, mFont.CENTER);

			// Nút 3: Goback
			int btnGobackX = btnSkillX + btnW + gap;
			g.setColor(2593000);
			g.fillRect(btnGobackX, btnActionY, btnW, btnH);
			g.setColor(2758924);
			g.drawRect(btnGobackX, btnActionY, btnW, btnH);
			mFont.tahoma_7b_white.drawString(g, "Goback", btnGobackX + btnW / 2, btnActionY + 4, mFont.CENTER);

			// Nút 4: Xmap
			int btnXmapX = btnGobackX + btnW + gap;
			g.setColor(2593000);
			g.fillRect(btnXmapX, btnActionY, btnW, btnH);
			g.setColor(2758924);
			g.drawRect(btnXmapX, btnActionY, btnW, btnH);
			mFont.tahoma_7b_white.drawString(g, "Xmap", btnXmapX + btnW / 2, btnActionY + 4, mFont.CENTER);
		}
		// -------------------------------------------------------------
		// TAB 1: 👑 SĂN BOSS (TITLE RIÊNG THÔNG BÁO BOSS)
		// -------------------------------------------------------------
		else if (activeMainDialogTab == 1)
		{
			int startY = contentY + 6;
			drawCheckboxRow(g, boxX + 16, startY, "Thông báo Boss xuất hiện (Chat: tb)", BossManager.isShowBossNotice);

			mFont.tahoma_7b_dark.drawString(g, "• Thông báo danh sách Boss xuất hiện dưới nút Chat.", boxX + 16, startY + 22, 0);
			mFont.tahoma_7b_dark.drawString(g, "• Đổi màu đỏ đậm khi Boss ra cùng map với nhân vật.", boxX + 16, startY + 38, 0);
			mFont.tahoma_7b_dark.drawString(g, "• Vẽ sợi chỉ vàng laser nối thẳng tới Boss khi cùng khu.", boxX + 16, startY + 54, 0);
			mFont.tahoma_7b_dark.drawString(g, "• Chat nhanh 'tb' để bật / tắt tức thì.", boxX + 16, startY + 70, 0);

			int btnW = boxW - 32;
			int btnH = 22;
			int btnY = startY + 92;
			int btnX = boxX + 16;
			g.setColor(BossManager.isShowBossNotice ? 3058980 : 1800936);
			g.fillRect(btnX, btnY, btnW, btnH);
			g.setColor(2758924);
			g.drawRect(btnX, btnY, btnW, btnH);
			mFont.tahoma_7b_white.drawString(g, BossManager.isShowBossNotice ? "🔔 Trạng Thái: ĐANG BẬT (tb)" : "🔕 Trạng Thái: ĐANG TẮT (tb)", btnX + btnW / 2, btnY + 4, mFont.CENTER);
		}
		// -------------------------------------------------------------
		// TAB 2: 👥 TRONG KHU (TITLE RIÊNG NGƯỜI & BOSS TRONG KHU)
		// -------------------------------------------------------------
		else if (activeMainDialogTab == 2)
		{
			int startY = contentY + 6;
			drawCheckboxRow(g, boxX + 16, startY, "Hiện người và Boss trong khu (Chat: nk)", BossManager.isShowCharInMap);

			mFont.tahoma_7b_dark.drawString(g, "• Hiển thị danh sách Boss & Người chơi trong map hiện tại.", boxX + 16, startY + 22, 0);
			mFont.tahoma_7b_dark.drawString(g, "• Cập nhật lượng máu HP real-time của người & Boss.", boxX + 16, startY + 38, 0);
			mFont.tahoma_7b_dark.drawString(g, "• Phông chữ nhỏ gọn tahoma_7 chuẩn theo giao diện map.", boxX + 16, startY + 54, 0);
			mFont.tahoma_7b_dark.drawString(g, "• Chat nhanh 'nk' để bật / tắt tức thì.", boxX + 16, startY + 70, 0);

			int btnW = boxW - 32;
			int btnH = 22;
			int btnY = startY + 92;
			int btnX = boxX + 16;
			g.setColor(BossManager.isShowCharInMap ? 3058980 : 1800936);
			g.fillRect(btnX, btnY, btnW, btnH);
			g.setColor(2758924);
			g.drawRect(btnX, btnY, btnW, btnH);
			mFont.tahoma_7b_white.drawString(g, BossManager.isShowCharInMap ? "👥 Trạng Thái: ĐANG BẬT (nk)" : "👤 Trạng Thái: ĐANG TẮT (nk)", btnX + btnW / 2, btnY + 4, mFont.CENTER);
		}
		// -------------------------------------------------------------
		// TAB 3: 📦 TỰ NHẶT & VỨT ĐỒ
		// -------------------------------------------------------------
		else if (activeMainDialogTab == 3)
		{
			int rowH = 18;
			int startY = contentY + 4;

			drawCheckboxRow(g, boxX + 16, startY, "Tự động nhặt vật phẩm", isAutoPick);
			drawCheckboxRow(g, boxX + 16, startY + rowH, "Bùa thu hút (Hút đồ nhặt xa)", isBuaThuHut);
			drawCheckboxRow(g, boxX + 16, startY + rowH * 2, "Nhặt vật phẩm của người khác", isPickOthers);
			drawCheckboxRow(g, boxX + 16, startY + rowH * 3, "Nhặt toàn map (Dịch chuyển nhặt)", isPickAllMap);
			drawCheckboxRow(g, boxX + 16, startY + rowH * 4, "Tự động vứt vật phẩm rác", isAutoDrop);

			// Nút Cài Đặt Nhặt & Vứt Chi Tiết (Full width)
			int btnPickW = boxW - 32;
			int btnPickH = 20;
			int btnPickY = startY + rowH * 5 + 2;
			int btnPickX = boxX + 16;

			g.setColor(1800936);
			g.fillRect(btnPickX, btnPickY, btnPickW, btnPickH);
			g.setColor(2758924);
			g.drawRect(btnPickX, btnPickY, btnPickW, btnPickH);
			mFont.tahoma_7b_white.drawString(g, "📦 Cài Đặt Nhặt & Vứt Chi Tiết (N)", btnPickX + btnPickW / 2, btnPickY + 4, mFont.CENTER);
		}
		// -------------------------------------------------------------
		// TAB 4: 🍗 AUTO ITEM (DÙNG ĐỒ ĂN, BÙA, ĐẬU, BUFF)
		// -------------------------------------------------------------
		else if (activeMainDialogTab == 4)
		{
			int startY = contentY + 8;
			drawCheckboxRow(g, boxX + 16, startY, "Tự động dùng Item (Auto Item)", isAutoItem);

			mFont.tahoma_7b_dark.drawString(g, "• Tự dùng: Cuồng nộ, Bổ huyết, Giáp Xên, Đồ ăn,", boxX + 16, startY + 24, 0);
			mFont.tahoma_7b_dark.drawString(g, "  Ẩn danh, Nho tím, Nho xanh, Bùa, Tự luyện tập...", boxX + 16, startY + 38, 0);
			mFont.tahoma_7b_dark.drawString(g, "• Tự kích hoạt lại theo chu kỳ khi hết hiệu lực.", boxX + 16, startY + 52, 0);

			// Nút mở Bảng Auto Item 2 Cột
			int btnItemW = boxW - 32;
			int btnItemH = 22;
			int btnItemY = startY + 68;
			int btnItemX = boxX + 16;

			g.setColor(1800936);
			g.fillRect(btnItemX, btnItemY, btnItemW, btnItemH);
			g.setColor(2758924);
			g.drawRect(btnItemX, btnItemY, btnItemW, btnItemH);
			mFont.tahoma_7b_white.drawString(g, "🍗 Mở Bảng Cài Auto Item 2 Cột", btnItemX + btnItemW / 2, btnItemY + 5, mFont.CENTER);
		}
		// -------------------------------------------------------------
		// TAB 5: 🎨 CÀI ĐẶT ĐỒ HOẠ (THẤP / TRUNG BÌNH / CAO)
		// -------------------------------------------------------------
		else if (activeMainDialogTab == 5)
		{
			int startY = contentY + 6;
			mFont.tahoma_7b_dark.drawString(g, "Cấu hình đồ họa game (Tối ưu độ mượt & giảm lag):", boxX + 16, startY, 0);

			// 3 Nút chọn cấu hình đồ họa: [ Thấp ] - [ Trung Bình ] - [ Cao ]
			int btnH = 22;
			int btnW = (boxW - 32 - 12) / 3;
			int btnY = startY + 16;

			string[] gfxLabels = new string[] { "🟢 THẤP", "🟡 TRUNG BÌNH", "🔴 CAO" };
			for (int i = 0; i < 3; i++)
			{
				int bx = boxX + 16 + i * (btnW + 6);
				bool isSelected = (GraphicsOptimizer.graphicsLevel == i);
				g.setColor(isSelected ? 3058980 : 1800936);
				g.fillRect(bx, btnY, btnW, btnH);
				g.setColor(isSelected ? 16777215 : 2758924);
				g.drawRect(bx, btnY, btnW, btnH);
				mFont.tahoma_7b_white.drawString(g, gfxLabels[i], bx + btnW / 2, btnY + 4, mFont.CENTER);
			}

			// Mô tả chi tiết mức đồ họa đang chọn
			int descY = btnY + btnH + 8;
			if (GraphicsOptimizer.graphicsLevel == 0)
			{
				mFont.tahoma_7b_dark.drawString(g, "• Mức THẤP: Xóa background cảnh xa đồi núi, phủ nền phẳng mượt mà.", boxX + 16, descY, 0);
				mFont.tahoma_7b_dark.drawString(g, "• Tắt hiệu ứng dư thừa: Hào quang, sương mù, mây dày, tia nắng, bụi chân.", boxX + 16, descY + 16, 0);
				mFont.tahoma_7b_dark.drawString(g, "• Khóa cứng FPS ổn định, chống drop FPS và giật lag cho máy yếu.", boxX + 16, descY + 32, 0);
				mFont.tahoma_7b_dark.drawString(g, "• Bảo toàn 100% chuyển động nhân vật, quái, NPC, tàu xe và chiêu thức.", boxX + 16, descY + 48, 0);
			}
			else if (GraphicsOptimizer.graphicsLevel == 1)
			{
				mFont.tahoma_7b_dark.drawString(g, "• Mức TRUNG BÌNH: Cân bằng hoàn hảo giữa đồ họa và độ mượt mà.", boxX + 16, descY, 0);
				mFont.tahoma_7b_dark.drawString(g, "• Hiển thị đầy đủ hình ảnh, hào quang và hoạt cảnh tự nhiên.", boxX + 16, descY + 16, 0);
				mFont.tahoma_7b_dark.drawString(g, "• Khuyên dùng cho hầu hết các máy tính chơi game ổn định.", boxX + 16, descY + 32, 0);
			}
			else
			{
				mFont.tahoma_7b_dark.drawString(g, "• Mức CAO: Đồ họa đầy đủ chi tiết, sinh động và sắc nét nhất.", boxX + 16, descY, 0);
				mFont.tahoma_7b_dark.drawString(g, "• Hiển thị trọn vẹn hào quang, hiệu ứng chiêu thức và cảnh quan bản đồ.", boxX + 16, descY + 16, 0);
				mFont.tahoma_7b_dark.drawString(g, "• Phù hợp máy tính có cấu hình khỏe muốn trải nghiệm hình ảnh tốt nhất.", boxX + 16, descY + 32, 0);
			}
		}
		// -------------------------------------------------------------
		// TAB 6: ⚡ CÀI ĐẶT FPS (60, 90, 120, 144)
		// -------------------------------------------------------------
		else if (activeMainDialogTab == 6)
		{
			int startY = contentY + 6;
			mFont.tahoma_7b_dark.drawString(g, "Mục tiêu FPS (Mặc định 120 FPS mượt mà):", boxX + 16, startY, 0);

			// 4 Nút chọn FPS: [ 60 FPS ] - [ 90 FPS ] - [ 120 FPS ] - [ 144 FPS ]
			int btnH = 22;
			int btnW = (boxW - 32 - 18) / 4;
			int btnY = startY + 16;
			int[] fpsList = new int[] { 60, 90, 120, 144 };

			for (int i = 0; i < 4; i++)
			{
				int bx = boxX + 16 + i * (btnW + 6);
				bool isSelected = (GraphicsOptimizer.selectedFps == fpsList[i]);
				g.setColor(isSelected ? 3058980 : 1800936);
				g.fillRect(bx, btnY, btnW, btnH);
				g.setColor(isSelected ? 16777215 : 2758924);
				g.drawRect(bx, btnY, btnW, btnH);
				mFont.tahoma_7b_white.drawString(g, fpsList[i] + " FPS", bx + btnW / 2, btnY + 4, mFont.CENTER);
			}

			// Checkbox 1: Tự bóp FPS thông minh
			int chkY1 = btnY + btnH + 8;
			drawCheckboxRow(g, boxX + 16, chkY1, "Tự bóp FPS thông minh theo máy (Dynamic FPS chống nóng)", GraphicsOptimizer.isDynamicFpsEnabled);

			// Checkbox 2: Giảm xuống 30 FPS khi tab ra ngoài
			int chkY2 = chkY1 + 17;
			drawCheckboxRow(g, boxX + 16, chkY2, "Giảm xuống 30 FPS khi tab ra ngoài (Tiết kiệm CPU)", GraphicsOptimizer.isThrottleBackgroundFps);

			int descY = chkY2 + 18;
			mFont.tahoma_7b_dark.drawString(g, "• FPS hiện tại: " + GraphicsOptimizer.currentFpsDisplay + " FPS (Cài đặt: " + GraphicsOptimizer.selectedFps + " FPS).", boxX + 16, descY, 0);
			mFont.tahoma_7b_dark.drawString(g, "• Mặc định luôn giữ vững " + GraphicsOptimizer.selectedFps + " FPS mượt mà khi chuyển tab / chạy nhiều acc.", boxX + 16, descY + 13, 0);
			mFont.tahoma_7b_dark.drawString(g, "• Dynamic FPS tự động giảm nhẹ frame rate khi máy bị quá tải,", boxX + 16, descY + 26, 0);
			mFont.tahoma_7b_dark.drawString(g, "  giúp quạt laptop không rú, CPU không bị quá nhiệt và lag.", boxX + 16, descY + 39, 0);
		}
		// -------------------------------------------------------------
		// TAB 7: ⚙ HỆ THỐNG & TỐC ĐỘ GAME
		// -------------------------------------------------------------
		else if (activeMainDialogTab == 7)
		{
			int startY = contentY + 8;
			drawCheckboxRow(g, boxX + 16, startY, "Hiện bảng HUD trạng thái màn hình", isShowHUD);

			// Tiêu đề Tốc độ
			int spdTitleY = startY + 24;
			mFont.tahoma_7b_dark.drawString(g, "Tốc độ game: x" + currentSpeed, boxX + 16, spdTitleY + 2, 0);

			// Nút [-] và [+]
			int btnMiniW = 24;
			int btnMiniH = 18;
			int btnMinusX = boxX + 225;
			int btnPlusX = boxX + 260;

			g.setColor(1800936);
			g.fillRect(btnMinusX, spdTitleY, btnMiniW, btnMiniH);
			g.setColor(2758924);
			g.drawRect(btnMinusX, spdTitleY, btnMiniW, btnMiniH);
			mFont.tahoma_7b_white.drawString(g, "-", btnMinusX + btnMiniW / 2, spdTitleY + 1, mFont.CENTER);

			g.setColor(1800936);
			g.fillRect(btnPlusX, spdTitleY, btnMiniW, btnMiniH);
			g.setColor(2758924);
			g.drawRect(btnPlusX, spdTitleY, btnMiniW, btnMiniH);
			mFont.tahoma_7b_white.drawString(g, "+", btnPlusX + btnMiniW / 2, spdTitleY + 1, mFont.CENTER);

			// 2 Hàng nút tốc độ (1x -> 10x)
			float[] speeds = new float[] { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f };
			string[] speedLabels = new string[] { "1x", "2x", "3x", "4x", "5x", "6x", "7x", "8x", "9x", "10x" };
			int btnSpdW = (boxW - 32 - 16) / 5;
			int btnSpdH = 18;
			int gap = 4;
			int startBtnSpdX = boxX + 16;
			int btnSpdY1 = spdTitleY + 24;
			int btnSpdY2 = btnSpdY1 + btnSpdH + 4;

			// Hàng 1 (1x -> 5x)
			for (int i = 0; i < 5; i++)
			{
				int curBtnX = startBtnSpdX + i * (btnSpdW + gap);
				bool isCur = Mathf.Abs(currentSpeed - speeds[i]) < 0.05f;
				g.setColor(isCur ? 3058980 : 1800936);
				g.fillRect(curBtnX, btnSpdY1, btnSpdW, btnSpdH);
				g.setColor(2758924);
				g.drawRect(curBtnX, btnSpdY1, btnSpdW, btnSpdH);
				mFont.tahoma_7b_white.drawString(g, speedLabels[i], curBtnX + btnSpdW / 2, btnSpdY1 + 3, mFont.CENTER);
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
				mFont.tahoma_7b_white.drawString(g, speedLabels[j], curBtnX + btnSpdW / 2, btnSpdY2 + 3, mFont.CENTER);
			}
		}

		// 4. NÚT [ ĐÓNG BẢNG (ESC / X) ] DƯỚI ĐÁY BẢNG
		int btnCloseBottomW = 160;
		int btnCloseBottomH = 19;
		int btnCloseBottomX = boxX + (boxW - btnCloseBottomW) / 2;
		int btnCloseBottomY = boxY + boxH - 22;
		g.setColor(13382451);
		g.fillRect(btnCloseBottomX, btnCloseBottomY, btnCloseBottomW, btnCloseBottomH);
		g.setColor(2758924);
		g.drawRect(btnCloseBottomX, btnCloseBottomY, btnCloseBottomW, btnCloseBottomH);
		mFont.tahoma_7b_white.drawString(g, "✕ ĐÓNG BẢNG (ESC / X)", btnCloseBottomX + btnCloseBottomW / 2, btnCloseBottomY + 3, mFont.CENTER);
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
		int btnEditW = 32;
		int tfEatX = boxX + 118;
		int tfEatW = boxW - 124 - btnEditW - 6;
		int tfEatH = 18;
		int btnEatEditX = tfEatX + tfEatW + 4;
		drawInputField(g, tfEatX, inputEatY, tfEatW, tfEatH, eatItemFilter, "mỗi ItemID cách nhau ','", activeInputIndex == 3, 3);
		drawEditButton(g, btnEatEditX, inputEatY, btnEditW, tfEatH);

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

		// Tooltip hiển thị toàn bộ chuỗi số khi rê chuột
		if (!string.IsNullOrEmpty(hoveredTooltipText))
		{
			int tw = mFont.tahoma_7b_white.getWidth(hoveredTooltipText) + 12;
			int th = 17;
			int tx = hoveredTooltipX;
			if (tx + tw > GameCanvas.w - 8) tx = GameCanvas.w - tw - 8;
			if (tx < 8) tx = 8;
			int ty = hoveredTooltipY;
			if (ty + th > GameCanvas.h - 8) ty = hoveredTooltipY - 24 - th;

			g.setColor(0, 0.92f);
			g.fillRect(tx, ty, tw, th);
			g.setColor(16776960);
			g.drawRect(tx, ty, tw, th);
			mFont.tahoma_7b_yellow.drawString(g, hoveredTooltipText, tx + 6, ty + 3, 0);
			hoveredTooltipText = null;
		}
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

	public static void drawTriangleUp(mGraphics g, int x, int y)
	{
		g.setColor(1800936);
		for (int i = 0; i < 6; i++) { g.fillRect(x - i, y + i, i * 2 + 1, 1); }
	}

	public static void drawTriangleDown(mGraphics g, int x, int y)
	{
		g.setColor(1800936);
		for (int i = 0; i < 6; i++) { g.fillRect(x - (5 - i), y + i, (5 - i) * 2 + 1, 1); }
	}

	// =============================================================
	// 4. VẼ BẢNG AUTO NHẶT CHUẨN THEO HÌNH ẢNH YÊU CẦU
	// =============================================================
	private static void paintPickDialog(mGraphics g)
	{
		int boxW = 310;
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

		// 4. 3 Ô NHẬP TEXTBOX D/S ITEMID CÙNG NÚT [SỬA] ĐỂ XEM/SỬA TOÀN BỘ
		int inputStartY = startY + radioRowH * 4 + 6;
		int inputRowH = 24;
		int btnEditW = 32;
		int tfX = boxX + 115;
		int tfW = boxW - 120 - btnEditW - 6;
		int tfH = 19;
		int btnEditX = tfX + tfW + 4;

		// Dòng 1: D/S ItemID Nhặt
		mFont.tahoma_7b_dark.drawString(g, "D/S ItemID Nhặt:", boxX + 12, inputStartY + 3, 0);
		drawInputField(g, tfX, inputStartY, tfW, tfH, pickItemFilter, "cách nhau dấu ','", activeInputIndex == 0, 0);
		drawEditButton(g, btnEditX, inputStartY, btnEditW, tfH);

		// Dòng 2: D/S ItemID Loại Trừ
		mFont.tahoma_7b_dark.drawString(g, "D/S ItemID Loại Trừ:", boxX + 12, inputStartY + inputRowH + 3, 0);
		drawInputField(g, tfX, inputStartY + inputRowH, tfW, tfH, excludeItemFilter, "cách nhau dấu ','", activeInputIndex == 1, 1);
		drawEditButton(g, btnEditX, inputStartY + inputRowH, btnEditW, tfH);

		// Dòng 3: D/S ItemID Vứt Bỏ
		mFont.tahoma_7b_dark.drawString(g, "D/S ItemID Vứt Bỏ:", boxX + 12, inputStartY + inputRowH * 2 + 3, 0);
		drawInputField(g, tfX, inputStartY + inputRowH * 2, tfW, tfH, dropItemFilter, "cách nhau dấu ','", activeInputIndex == 2, 2);
		drawEditButton(g, btnEditX, inputStartY + inputRowH * 2, btnEditW, tfH);

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

		// 6. Tooltip hiển thị toàn bộ chuỗi số khi rê chuột
		if (!string.IsNullOrEmpty(hoveredTooltipText))
		{
			int tw = mFont.tahoma_7b_white.getWidth(hoveredTooltipText) + 12;
			int th = 17;
			int tx = hoveredTooltipX;
			if (tx + tw > GameCanvas.w - 8) tx = GameCanvas.w - tw - 8;
			if (tx < 8) tx = 8;
			int ty = hoveredTooltipY;
			if (ty + th > GameCanvas.h - 8) ty = hoveredTooltipY - 24 - th;

			g.setColor(0, 0.92f);
			g.fillRect(tx, ty, tw, th);
			g.setColor(16776960);
			g.drawRect(tx, ty, tw, th);
			mFont.tahoma_7b_yellow.drawString(g, hoveredTooltipText, tx + 6, ty + 3, 0);
			hoveredTooltipText = null;
		}
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

	private static string hoveredTooltipText = null;
	private static int hoveredTooltipX = 0;
	private static int hoveredTooltipY = 0;
	public static int[] inputFieldScrollOffsets = new int[5];

	// Vẽ nút [Sửa] nhỏ cạnh ô nhập để mở bảng nhập toàn bộ chuỗi số
	private static void drawEditButton(mGraphics g, int x, int y, int w, int h)
	{
		g.setColor(1800936);
		g.fillRect(x, y, w, h);
		g.setColor(2758924);
		g.drawRect(x, y, w, h);
		mFont.tahoma_7b_white.drawString(g, "Sửa", x + w / 2, y + 4, mFont.CENTER);
	}

	// Vẽ ô nhập InputField vàng cam viền bo góc, hỗ trợ cuộn chữ mượt mà khi chuỗi dài & nhấp nháy con trỏ
	private static void drawInputField(mGraphics g, int x, int y, int w, int h, string content, string placeholder, bool isFocus = false, int fieldIndex = -1)
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
			int textW = font.getWidth(text);
			int visibleW = w - 12;
			int offset = 0;

			if (textW > visibleW)
			{
				int maxOffset = textW - visibleW;
				if (isFocus)
				{
					// Khi đang focus gõ: Tự động cuộn theo con trỏ để luôn thấy số mới gõ
					offset = maxOffset;
					if (fieldIndex >= 0 && fieldIndex < inputFieldScrollOffsets.Length)
					{
						inputFieldScrollOffsets[fieldIndex] = maxOffset;
					}
				}
				else
				{
					if (fieldIndex >= 0 && fieldIndex < inputFieldScrollOffsets.Length)
					{
						offset = Mathf.Clamp(inputFieldScrollOffsets[fieldIndex], 0, maxOffset);
					}
				}
			}

			int drawX = x + 5 - offset;
			font.drawString(g, text, drawX, y + 3, 0);

			// Vẽ con trỏ nhấp nháy '|' khi đang focus nhập liệu (luôn nằm trong ô nhìn thấy được)
			if (isFocus && (mSystem.currentTimeMillis() / 400 % 2 == 0))
			{
				int caretX = drawX + textW;
				if (caretX > x + w - 6) caretX = x + w - 6;
				g.setColor(0);
				g.fillRect(caretX, y + 3, 1, 13);
			}

			// Kiểm tra chuột rê qua ô này để bật Tooltip hiển thị trọn vẹn toàn bộ chuỗi số
			try
			{
				int mouseX = (int)(Input.mousePosition.x / (float)mGraphics.zoomLevel);
				int mouseY = (int)(((float)Screen.height - Input.mousePosition.y) / (float)mGraphics.zoomLevel);
				if (textW > visibleW && mouseX >= x && mouseX <= x + w && mouseY >= y && mouseY <= y + h)
				{
					hoveredTooltipText = text;
					hoveredTooltipX = x;
					hoveredTooltipY = y + h + 2;
				}
			}
			catch (Exception)
			{
			}
		}
		GameCanvas.resetTrans(g);
	}

	// =============================================================
	// XỬ LÝ CLICK CHUỘT / TOUCH TRÊN TẤT CẢ CÁC BẢNG POPUP
	// =============================================================
	public static void updateDialogInput()
	{
		// Xử lý gõ phím trực tiếp vào ô Textbox trong Bảng Auto Nhặt / Auto Item / Goback

		// Xử lý gõ phím trực tiếp vào ô Textbox trong Bảng Auto Nhặt / Auto Item / Goback
		if ((isShowPickDialog || isShowItemDialog || isShowGobackDialog) && activeInputIndex >= 0)
		{
			try
			{
				if (Time.frameCount != lastInputProcessedFrame)
				{
					lastInputProcessedFrame = Time.frameCount;
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
								if (activeInputIndex == 4)
								{
									applyGobackPosFromText();
									XmapManager.hasSavedGobackPos = true;
									notify("Đã áp dụng toạ độ: " + gobackPosText);
								}
								activeInputIndex = -1;
								clearDropBlacklist();
								saveSettings();
								notify("Đã lưu cài đặt!");
							}
							else if (char.IsDigit(c) || c == ',' || c == ' ' || c == ';' || (activeInputIndex == 4 && c == '-'))
							{
								handleInputChar(c);
							}
						}
					}

					// Hỗ trợ Copy sang Clipboard (Ctrl + C)
					if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
					{
						if (activeInputIndex == 4 && !string.IsNullOrEmpty(gobackPosText))
						{
							GUIUtility.systemCopyBuffer = gobackPosText;
							notify("Đã copy toạ độ: " + gobackPosText);
						}
					}

					// Hỗ trợ Paste từ Clipboard (Ctrl + V)
					if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
					{
						string clip = GUIUtility.systemCopyBuffer;
						if (!string.IsNullOrEmpty(clip))
						{
							if (activeInputIndex == 4)
							{
								gobackPosText = clip.Trim();
								applyGobackPosFromText();
								notify("Đã dán toạ độ: " + gobackPosText);
							}
							else
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
				}
			}
			catch (Exception)
			{
			}
		}

		// Hỗ trợ cuộn chuột xem các chuỗi số dài trong các ô Textbox
		try
		{
			float wheel = Input.GetAxis("Mouse ScrollWheel");
			if (Mathf.Abs(wheel) > 0.02f)
			{
				int mouseX = (int)(Input.mousePosition.x / (float)mGraphics.zoomLevel);
				int mouseY = (int)(((float)Screen.height - Input.mousePosition.y) / (float)mGraphics.zoomLevel);

				if (isShowPickDialog)
				{
					int bW = 310;
					int bH = 220;
					int bX = GameCanvas.w / 2 - bW / 2;
					int bY = GameCanvas.h / 2 - bH / 2;
					int inStartY = bY + 24 + 6 + 20 * 4 + 6;
					int inTfX = bX + 115;
					int inTfW = bW - 120 - 32 - 6;

					for (int f = 0; f < 3; f++)
					{
						int curY = inStartY + f * 24;
						if (mouseX >= inTfX && mouseX <= inTfX + inTfW && mouseY >= curY && mouseY <= curY + 20)
						{
							if (wheel > 0.02f) inputFieldScrollOffsets[f] = Mathf.Max(0, inputFieldScrollOffsets[f] - 30);
							else if (wheel < -0.02f) inputFieldScrollOffsets[f] += 30;
							break;
						}
					}
				}
				else if (isShowItemDialog)
				{
					int bW = 275;
					int bH = 245;
					int bX = GameCanvas.w / 2 - bW / 2;
					int bY = GameCanvas.h / 2 - bH / 2;
					int inEatY = bY + 24 + 5 + 8 * 17 + 4;
					int inEatX = bX + 118;
					int inEatW = bW - 124 - 32 - 6;
					if (mouseX >= inEatX && mouseX <= inEatX + inEatW && mouseY >= inEatY && mouseY <= inEatY + 20)
					{
						if (wheel > 0.02f) inputFieldScrollOffsets[3] = Mathf.Max(0, inputFieldScrollOffsets[3] - 30);
						else if (wheel < -0.02f) inputFieldScrollOffsets[3] += 30;
					}
				}
			}
		}
		catch (Exception)
		{
		}

		if (isShowMobDialog)
		{
			handleMobDialogInput();
			return;
		}

		if (isShowGobackDialog)
		{
			handleGobackDialogInput();
			return;
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

		if (XmapManager.isShowXmapDialog)
		{
			XmapManager.handleXmapDialogInput();
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
		int boxW = 310;
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
				closeAllDialogs();
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

			// 3. Click 3 ô Textbox nhập ItemID hoặc nút [Sửa]
			int inputStartY = startY + radioRowH * 4 + 6;
			int inputRowH = 24;
			int btnEditW = 32;
			int tfX = boxX + 115;
			int tfW = boxW - 120 - btnEditW - 6;
			int btnEditX = tfX + tfW + 4;

			if (px >= boxX + 10 && px <= boxX + boxW - 10)
			{
				// Ô 1: D/S ItemID Nhặt
				if (py >= inputStartY && py < inputStartY + inputRowH)
				{
					if ((px >= btnEditX && px <= btnEditX + btnEditW) || px < tfX)
					{
						GameCanvas.inputDlg.show("Nhập D/S ItemID Nhặt (cách nhau dấu phẩy):", new Command("Lưu", ModManager.gI(), 10001, null), TField.INPUT_TYPE_ANY);
						if (GameCanvas.inputDlg.tfInput != null) GameCanvas.inputDlg.tfInput.setText(pickItemFilter);
					}
					else
					{
						activeInputIndex = 0;
					}
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Ô 2: D/S ItemID Loại Trừ
				else if (py >= inputStartY + inputRowH && py < inputStartY + inputRowH * 2)
				{
					if ((px >= btnEditX && px <= btnEditX + btnEditW) || px < tfX)
					{
						GameCanvas.inputDlg.show("Nhập D/S ItemID Loại Trừ (cách nhau dấu phẩy):", new Command("Lưu", ModManager.gI(), 10002, null), TField.INPUT_TYPE_ANY);
						if (GameCanvas.inputDlg.tfInput != null) GameCanvas.inputDlg.tfInput.setText(excludeItemFilter);
					}
					else
					{
						activeInputIndex = 1;
					}
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Ô 3: D/S ItemID Vứt Bỏ
				else if (py >= inputStartY + inputRowH * 2 && py < inputStartY + inputRowH * 3)
				{
					if ((px >= btnEditX && px <= btnEditX + btnEditW) || px < tfX)
					{
						GameCanvas.inputDlg.show("Nhập D/S ItemID Vứt Bỏ (cách nhau dấu phẩy):", new Command("Lưu", ModManager.gI(), 10003, null), TField.INPUT_TYPE_ANY);
						if (GameCanvas.inputDlg.tfInput != null) GameCanvas.inputDlg.tfInput.setText(dropItemFilter);
					}
					else
					{
						activeInputIndex = 2;
					}
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

	// Helper lấy tên quái an toàn
	public static string getMobName(Mob mob)
	{
		try
		{
			if (mob != null)
			{
				MobTemplate t = mob.getTemplate();
				if (t != null && !string.IsNullOrEmpty(t.name))
				{
					return t.name;
				}
				if (Mob.arrMobTemplate != null && mob.templateId >= 0 && mob.templateId < Mob.arrMobTemplate.Length && Mob.arrMobTemplate[mob.templateId] != null)
				{
					return Mob.arrMobTemplate[mob.templateId].name;
				}
			}
		}
		catch (Exception)
		{
		}
		return "Quái";
	}

	// =============================================================
	// VẼ BẢNG CHỌN QUÁI THEO VỊ TRÍ (AUTO TRAIN - HÌNH ẢNH MẪU YÊU CẦU)
	// =============================================================
	private static void paintMobDialog(mGraphics g)
	{
		int boxW = 260;
		int boxH = 230;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;
		int headerH = 24;

		// 1. Nền vàng cát sa mạc
		g.setColor(14860437);
		g.fillRect(boxX, boxY, boxW, boxH);
		g.setColor(2758924);
		g.drawRect(boxX, boxY, boxW, boxH);

		// 2. Tiêu đề xanh dương: Auto Train
		g.setColor(2593000);
		g.fillRect(boxX, boxY, boxW, headerH);
		mFont.tahoma_7b_white.drawString(g, "Auto Train", boxX + boxW / 2, boxY + 5, mFont.CENTER);

		// Nút [X] đóng góc trên bên phải
		int btnCloseX = boxX + boxW - 18;
		int btnCloseY = boxY + 4;
		g.setColor(13382451);
		g.fillRect(btnCloseX, btnCloseY, 14, 15);
		g.setColor(16777215);
		g.drawRect(btnCloseX, btnCloseY, 14, 15);
		mFont.tahoma_7b_white.drawString(g, "X", btnCloseX + 7, btnCloseY + 2, mFont.CENTER);

		// 3. Tiêu đề con: Chọn Quái Theo Vị Trí:
		int titleY = boxY + headerH + 6;
		mFont.tahoma_7b_dark.drawString(g, "Chọn Quái Theo Vị Trí:", boxX + 12, titleY, 0);

		int totalMobs = (GameScr.vMob != null) ? GameScr.vMob.size() : 0;
		int maxVisibleRows = 8;
		int rowH = 18;
		int listStartY = titleY + 18;

		// Mũi tên cuộn lên / cuộn xuống
		if (scrollMobIndex > 0)
		{
			drawTriangleUp(g, boxX + boxW - 16, titleY + 3);
		}
		if (scrollMobIndex + maxVisibleRows < totalMobs)
		{
			drawTriangleDown(g, boxX + boxW - 16, titleY + 3 + maxVisibleRows * rowH);
		}

		if (totalMobs == 0)
		{
			mFont.tahoma_7_grey.drawString(g, "(Bản đồ này hiện không có quái)", boxX + 20, listStartY + 20, 0);
		}
		else
		{
			for (int i = 0; i < maxVisibleRows && (scrollMobIndex + i) < totalMobs; i++)
			{
				int idx = scrollMobIndex + i;
				Mob mob = (Mob)GameScr.vMob.elementAt(idx);
				if (mob == null) continue;

				int curY = listStartY + i * rowH;
				bool isSelected = selectedMobIds.Contains(mob.mobId);
				string label = getMobName(mob) + " (" + mob.mobId + " - " + mob.x + " - " + mob.y + ")";

				drawRadioButton(g, boxX + 16, curY, label, isSelected);
			}
		}

		// 4. Các nút bấm chức năng dưới đáy
		int btnBottomY = boxY + boxH - 24;
		int btnW = 75;
		int btnH = 18;

		// Nút [Chọn Tất Cả]
		int btnAllX = boxX + 10;
		g.setColor(1800936);
		g.fillRect(btnAllX, btnBottomY, btnW, btnH);
		g.setColor(2758924);
		g.drawRect(btnAllX, btnBottomY, btnW, btnH);
		mFont.tahoma_7b_white.drawString(g, "Chọn Hết", btnAllX + btnW / 2, btnBottomY + 3, mFont.CENTER);

		// Nút [Bỏ Chọn Hết]
		int btnClearX = boxX + 92;
		g.setColor(1800936);
		g.fillRect(btnClearX, btnBottomY, btnW, btnH);
		g.setColor(2758924);
		g.drawRect(btnClearX, btnBottomY, btnW, btnH);
		mFont.tahoma_7b_white.drawString(g, "Bỏ Chọn", btnClearX + btnW / 2, btnBottomY + 3, mFont.CENTER);

		// Nút [✕ Đóng]
		int btnCloseBottomX = boxX + 174;
		g.setColor(13382451);
		g.fillRect(btnCloseBottomX, btnBottomY, btnW, btnH);
		g.setColor(2758924);
		g.drawRect(btnCloseBottomX, btnBottomY, btnW, btnH);
		mFont.tahoma_7b_white.drawString(g, "✕ Đóng", btnCloseBottomX + btnW / 2, btnBottomY + 3, mFont.CENTER);
	}

	private static void handleMobDialogInput()
	{
		int boxW = 260;
		int boxH = 230;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;
		int headerH = 24;

		// Hỗ trợ cuộn chuột
		try
		{
			float wheel = Input.GetAxis("Mouse ScrollWheel");
			if (wheel > 0.05f && scrollMobIndex > 0)
			{
				scrollMobIndex--;
			}
			else if (wheel < -0.05f)
			{
				int total = (GameScr.vMob != null) ? GameScr.vMob.size() : 0;
				if (scrollMobIndex + 8 < total)
				{
					scrollMobIndex++;
				}
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

			// 1. Nút [X] đóng ở góc trên bên phải
			if (px >= boxX + boxW - 24 && px <= boxX + boxW && py >= boxY && py <= boxY + headerH)
			{
				closeAllDialogs();
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			int titleY = boxY + headerH + 6;
			int totalMobs = (GameScr.vMob != null) ? GameScr.vMob.size() : 0;
			int maxVisibleRows = 8;
			int rowH = 18;
			int listStartY = titleY + 18;

			// Click mũi tên cuộn lên
			if (px >= boxX + boxW - 28 && px <= boxX + boxW - 4 && py >= titleY - 2 && py <= titleY + 14)
			{
				if (scrollMobIndex > 0) scrollMobIndex--;
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Click mũi tên cuộn xuống
			if (px >= boxX + boxW - 28 && px <= boxX + boxW - 4 && py >= titleY + maxVisibleRows * rowH - 6 && py <= titleY + maxVisibleRows * rowH + 14)
			{
				if (scrollMobIndex + maxVisibleRows < totalMobs) scrollMobIndex++;
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Click vào từng dòng quái để toggle chọn/bỏ chọn
			if (px >= boxX + 10 && px <= boxX + boxW - 10)
			{
				for (int i = 0; i < maxVisibleRows && (scrollMobIndex + i) < totalMobs; i++)
				{
					int curY = listStartY + i * rowH;
					if (py >= curY && py < curY + rowH)
					{
						int idx = scrollMobIndex + i;
						Mob mob = (Mob)GameScr.vMob.elementAt(idx);
						if (mob != null)
						{
							string mName = getMobName(mob);
							if (selectedMobIds.Contains(mob.mobId))
							{
								selectedMobIds.Remove(mob.mobId);
								notify("Bỏ chọn: " + mName + " (" + mob.mobId + ")");
							}
							else
							{
								selectedMobIds.Add(mob.mobId);
								notify("Chọn pem: " + mName + " (" + mob.mobId + ")");
							}
							isFilterMob = (selectedMobIds.Count > 0);
						}
						GameCanvas.isPointerJustRelease = false;
						return;
					}
				}
			}

			// 4. Các nút dưới đáy
			int btnBottomY = boxY + boxH - 24;
			int btnW = 75;
			int btnH = 18;

			// Nút Chọn Hết
			int btnAllX = boxX + 10;
			if (px >= btnAllX && px <= btnAllX + btnW && py >= btnBottomY && py <= btnBottomY + btnH)
			{
				selectedMobIds.Clear();
				if (GameScr.vMob != null)
				{
					for (int m = 0; m < GameScr.vMob.size(); m++)
					{
						Mob mb = (Mob)GameScr.vMob.elementAt(m);
						if (mb != null) selectedMobIds.Add(mb.mobId);
					}
				}
				isFilterMob = true;
				notify("Đã chọn TẤT CẢ quái trong map!");
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Nút Bỏ Chọn
			int btnClearX = boxX + 92;
			if (px >= btnClearX && px <= btnClearX + btnW && py >= btnBottomY && py <= btnBottomY + btnH)
			{
				selectedMobIds.Clear();
				isFilterMob = false;
				notify("Đã BỎ CHỌN toàn bộ quái!");
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Nút ✕ Đóng
			int btnCloseBottomX = boxX + 174;
			if (px >= btnCloseBottomX && px <= btnCloseBottomX + btnW && py >= btnBottomY && py <= btnBottomY + btnH)
			{
				closeAllDialogs();
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Click ra ngoài bảng -> Đóng
			if (px < boxX || px > boxX + boxW || py < boxY || py > boxY + boxH)
			{
				closeAllDialogs();
			}

			GameCanvas.isPointerJustRelease = false;
		}
	}

	// =============================================================
	// VẼ BẢNG CÀI ĐẶT GOBACK & TOẠ ĐỘ RIÊNG BIỆT (THEO YÊU CẦU NGƯỜI DÙNG)
	// =============================================================
	private static void paintGobackDialog(mGraphics g)
	{
		int boxW = 280;
		int boxH = 246;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;
		int headerH = 24;

		// 1. Nền vàng cát sa mạc & viền đậm
		g.setColor(14860437);
		g.fillRect(boxX, boxY, boxW, boxH);
		g.setColor(2758924);
		g.drawRect(boxX, boxY, boxW, boxH);

		// 2. Tiêu đề xanh chính: CÀI ĐẶT GOBACK & TOẠ ĐỘ
		g.setColor(1800936);
		g.fillRect(boxX, boxY, boxW, headerH);
		mFont.tahoma_7b_white.drawString(g, "CÀI ĐẶT GOBACK & TOẠ ĐỘ", boxX + boxW / 2, boxY + 5, mFont.CENTER);

		// Nút [X] đóng ở góc trên bên phải
		int btnCloseX = boxX + boxW - 20;
		int btnCloseY = boxY + 4;
		g.setColor(13382451);
		g.fillRect(btnCloseX, btnCloseY, 16, 16);
		g.setColor(16777215);
		g.drawRect(btnCloseX, btnCloseY, 16, 16);
		mFont.tahoma_7b_white.drawString(g, "X", btnCloseX + 8, btnCloseY + 2, mFont.CENTER);

		// 3. Khung nội dung trắng sáng
		int contentY = boxY + headerH + 5;
		int contentH = 184;
		g.setColor(15720130);
		g.fillRect(boxX + 6, contentY, boxW - 12, contentH);
		g.setColor(2758924);
		g.drawRect(boxX + 6, contentY, boxW - 12, contentH);

		// 4. Các tuỳ chọn checkbox riêng biệt
		int startY = contentY + 6;
		int rowH = 18;

		// Ô 1: Tự động Goback về Map
		drawCheckboxRow(g, boxX + 14, startY, "Tự động Goback về Map (gb)", XmapManager.isGoback);

		// Ô 2: Về nhà khi hết KI (gk)
		drawCheckboxRow(g, boxX + 14, startY + rowH, "Về nhà khi hết KI (gk)", XmapManager.isGobackKi);

		// Ô 3: Khoá toạ độ đứng (Kéo về nếu lệch)
		drawCheckboxRow(g, boxX + 14, startY + rowH * 2, "Khoá toạ độ đứng (Kéo về nếu lệch)", XmapManager.hasSavedGobackPos);

		// 5. Ô nhập toạ độ mục tiêu & Nút [ 📌 Vị trí ]
		int inputTitleY = startY + rowH * 3 + 2;
		mFont.tahoma_7b_dark.drawString(g, "Nhập toạ độ (map, khu, x, y):", boxX + 14, inputTitleY, 0);

		int inputY = inputTitleY + 12;
		int tfH = 20;
		int btnGetW = 60;
		int tfW = boxW - 28 - btnGetW - 5;
		int tfX = boxX + 14;
		int btnGetX = tfX + tfW + 5;

		drawInputField(g, tfX, inputY, tfW, tfH, gobackPosText, "map, khu, x, y", activeInputIndex == 4, 4);

		g.setColor(2627855); // Xanh lá
		g.fillRect(btnGetX, inputY, btnGetW, tfH);
		g.setColor(2758924);
		g.drawRect(btnGetX, inputY, btnGetW, tfH);
		mFont.tahoma_7b_white.drawString(g, "📌 Vị trí", btnGetX + btnGetW / 2, inputY + 4, mFont.CENTER);

		// 6. Khung thông tin toạ độ đã lưu
		int infoY = inputY + tfH + 5;
		int infoW = boxW - 28;
		int infoH = 44;
		g.setColor(14860437);
		g.fillRect(boxX + 14, infoY, infoW, infoH);
		g.setColor(2758924);
		g.drawRect(boxX + 14, infoY, infoW, infoH);

		Char curChar = Char.myCharz();
		string liveMapName = TileMap.mapName;
		if (string.IsNullOrEmpty(liveMapName) || liveMapName == "0") liveMapName = XmapManager.getMapName(TileMap.mapID);
		int liveX = (curChar != null) ? curChar.cx : 0;
		int liveY = (curChar != null) ? curChar.cy : 0;

		// Dòng 1 & 2: Vị trí hiện tại Real-time của nhân vật
		string liveMapStr = "• Hiện tại: " + liveMapName + " [ID: " + TileMap.mapID + "] - Khu " + TileMap.zoneID;
		string livePosStr = "  Toạ độ đứng: (" + liveX + ", " + liveY + ")";
		mFont.tahoma_7_blue.drawString(g, liveMapStr, boxX + 20, infoY + 4, 0);
		mFont.tahoma_7_blue.drawString(g, livePosStr, boxX + 20, infoY + 16, 0);

		// Dòng 3: Trạng thái mục tiêu đã khoá
		if (XmapManager.hasSavedGobackPos && XmapManager.gobackMapId != -1)
		{
			string lockInfo = "🔒 Đang khoá: " + XmapManager.getMapName(XmapManager.gobackMapId) + " [K" + XmapManager.gobackZoneId + "] (" + XmapManager.gobackX + ", " + XmapManager.gobackY + ")";
			mFont.tahoma_7b_green.drawString(g, lockInfo, boxX + 20, infoY + 28, 0);
		}
		else
		{
			string targetInfo = "💡 Đề xuất: " + liveMapName + " [K" + TileMap.zoneID + "] (" + liveX + ", " + liveY + ")";
			mFont.tahoma_7_grey.drawString(g, targetInfo, boxX + 20, infoY + 28, 0);
		}

		// Trạng thái Goback đang chạy
		if (XmapManager.isReturningHomeForKi)
		{
			mFont.tahoma_7b_red.drawString(g, "⚡ Đang hồi KI", boxX + 14 + infoW - 6, infoY + 28, mFont.RIGHT);
		}
		else if (XmapManager.isGobacking)
		{
			mFont.tahoma_7b_green.drawString(g, "🚀 Đang Goback", boxX + 14 + infoW - 6, infoY + 28, mFont.RIGHT);
		}

		// 7. Hai nút thao tác Toạ Độ: [ 🔒 Áp dụng & Khoá ] & [ 🗑️ Xoá toạ độ ]
		int btnActionY = infoY + infoH + 5;
		int btnActionH = 20;
		int btnHalfW = (infoW - 6) / 2;

		// Nút Áp dụng toạ độ
		int btnSaveX = boxX + 14;
		g.setColor(1800936); // Xanh dương
		g.fillRect(btnSaveX, btnActionY, btnHalfW, btnActionH);
		g.setColor(2758924);
		g.drawRect(btnSaveX, btnActionY, btnHalfW, btnActionH);
		mFont.tahoma_7b_white.drawString(g, "🔒 Áp dụng & Khoá", btnSaveX + btnHalfW / 2, btnActionY + 3, mFont.CENTER);

		// Nút Xoá toạ độ
		int btnClearX = btnSaveX + btnHalfW + 6;
		g.setColor(11883572); // Đỏ gạch
		g.fillRect(btnClearX, btnActionY, btnHalfW, btnActionH);
		g.setColor(2758924);
		g.drawRect(btnClearX, btnActionY, btnHalfW, btnActionH);
		mFont.tahoma_7b_white.drawString(g, "🗑️ Xoá toạ độ", btnClearX + btnHalfW / 2, btnActionY + 3, mFont.CENTER);

		// 8. Nút ĐÓNG BẢNG (ESC / X)
		int btnCloseBottomY = boxY + boxH - 26;
		int btnCloseBottomH = 21;
		g.setColor(13382451);
		g.fillRect(boxX + 14, btnCloseBottomY, infoW, btnCloseBottomH);
		g.setColor(2758924);
		g.drawRect(boxX + 14, btnCloseBottomY, infoW, btnCloseBottomH);
		mFont.tahoma_7b_white.drawString(g, "ĐÓNG BẢNG (ESC / X)", boxX + boxW / 2, btnCloseBottomY + 3, mFont.CENTER);
	}

	private static void handleGobackDialogInput()
	{
		int boxW = 280;
		int boxH = 246;
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

			// 1. Nút [X] đóng bảng ở header
			if (px >= boxX + boxW - 24 && px <= boxX + boxW && py >= boxY && py <= boxY + headerH)
			{
				activeInputIndex = -1;
				closeAllDialogs();
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			int contentY = boxY + headerH + 5;
			int startY = contentY + 6;
			int rowH = 18;

			// 2. Ô 1: Tự động Goback về Map (gb)
			if (px >= boxX + 14 && px <= boxX + boxW - 14 && py >= startY && py < startY + rowH)
			{
				activeInputIndex = -1;
				XmapManager.toggleGoback();
				saveSettings();
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// 3. Ô 2: Về nhà khi hết KI (gk)
			if (px >= boxX + 14 && px <= boxX + boxW - 14 && py >= startY + rowH && py < startY + rowH * 2)
			{
				activeInputIndex = -1;
				XmapManager.toggleGobackKi();
				saveSettings();
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// 4. Ô 3: Khoá toạ độ đứng của nhân vật
			if (px >= boxX + 14 && px <= boxX + boxW - 14 && py >= startY + rowH * 2 && py < startY + rowH * 3)
			{
				activeInputIndex = -1;
				XmapManager.hasSavedGobackPos = !XmapManager.hasSavedGobackPos;
				if (XmapManager.hasSavedGobackPos)
				{
					if (XmapManager.gobackMapId == -1)
					{
						XmapManager.saveCurrentPosAsGoback();
					}
					else
					{
						XmapManager.isManualTravel = false;
						Pk9rXmap.IdMapCapsuleReturn = XmapManager.gobackMapId;
						notify("🔒 Đã khoá toạ độ đứng: " + XmapManager.getMapName(XmapManager.gobackMapId) + " [Khu " + XmapManager.gobackZoneId + "] (" + XmapManager.gobackX + ", " + XmapManager.gobackY + ")");
					}
				}
				else
				{
					if (!XmapManager.isGoback)
					{
						XmapManager.stopAllGobackAndXmap();
					}
					notify("Khoá toạ độ đứng: TẮT");
				}
				saveSettings();
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// 5. Ô nhập toạ độ & Nút [ 📌 Vị trí ]
			int inputTitleY = startY + rowH * 3 + 2;
			int inputY = inputTitleY + 12;
			int tfH = 20;
			int btnGetW = 60;
			int tfW = boxW - 28 - btnGetW - 5;
			int tfX = boxX + 14;
			int btnGetX = tfX + tfW + 5;

			// Click vào ô nhập text
			if (px >= tfX && px <= tfX + tfW && py >= inputY && py <= inputY + tfH)
			{
				activeInputIndex = 4;
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Click vào nút [ 📌 Vị trí ]
			if (px >= btnGetX && px <= btnGetX + btnGetW && py >= inputY && py <= inputY + tfH)
			{
				activeInputIndex = -1;
				Char cur = Char.myCharz();
				if (cur != null && !XmapManager.isHomeMap(TileMap.mapID))
				{
					gobackPosText = TileMap.mapID + ", " + TileMap.zoneID + ", " + cur.cx + ", " + cur.cy;
					XmapManager.gobackMapId = TileMap.mapID;
					XmapManager.gobackZoneId = TileMap.zoneID;
					XmapManager.gobackX = cur.cx;
					XmapManager.gobackY = cur.cy;
					notify("📌 Đã lấy toạ độ hiện tại: " + gobackPosText);
				}
				else if (XmapManager.isHomeMap(TileMap.mapID))
				{
					notify("⚠️ Không thể lưu toạ độ khi đang ở trong Nhà!");
				}
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			int infoY = inputY + tfH + 5;
			int infoH = 44;
			int infoW = boxW - 28;

			// 6. Hai nút thao tác Toạ Độ
			int btnActionY = infoY + infoH + 5;
			int btnActionH = 20;
			int btnHalfW = (infoW - 6) / 2;

			// Nút [ 🔒 Áp dụng & Khoá ]
			int btnSaveX = boxX + 14;
			if (px >= btnSaveX && px <= btnSaveX + btnHalfW && py >= btnActionY && py <= btnActionY + btnActionH)
			{
				activeInputIndex = -1;
				// Parse toạ độ từ ô nhập
				if (!string.IsNullOrEmpty(gobackPosText) && XmapManager.parseGobackPosText(gobackPosText, out int m, out int z, out int x, out int y))
				{
					XmapManager.gobackMapId = m;
					XmapManager.gobackZoneId = z;
					XmapManager.gobackX = x;
					XmapManager.gobackY = y;
					gobackPosText = m + ", " + z + ", " + x + ", " + y;
				}
				else
				{
					// Nếu chưa nhập: Lấy ngay toạ độ hiện tại của nhân vật
					Char cur = Char.myCharz();
					XmapManager.gobackMapId = TileMap.mapID;
					XmapManager.gobackZoneId = TileMap.zoneID;
					XmapManager.gobackX = (cur != null) ? cur.cx : 0;
					XmapManager.gobackY = (cur != null) ? cur.cy : 0;
					gobackPosText = XmapManager.gobackMapId + ", " + XmapManager.gobackZoneId + ", " + XmapManager.gobackX + ", " + XmapManager.gobackY;
				}

				// Khoá toạ độ đứng & Tự động Goback
				XmapManager.hasSavedGobackPos = true;
				XmapManager.isGoback = true;
				XmapManager.isManualTravel = false;
				if (XmapManager.gobackMapId != -1)
				{
					Pk9rXmap.IdMapCapsuleReturn = XmapManager.gobackMapId;
				}
				saveSettings();
				notify("🔒 Đã áp dụng & khoá toạ độ: " + gobackPosText);
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Nút [ 🗑️ Xoá toạ độ ]
			int btnClearX = btnSaveX + btnHalfW + 6;
			if (px >= btnClearX && px <= btnClearX + btnHalfW && py >= btnActionY && py <= btnActionY + btnActionH)
			{
				activeInputIndex = -1;
				XmapManager.clearSavedGobackPos();
				Char cur = Char.myCharz();
				int cx = (cur != null) ? cur.cx : 0;
				int cy = (cur != null) ? cur.cy : 0;
				gobackPosText = TileMap.mapID + ", " + TileMap.zoneID + ", " + cx + ", " + cy;
				saveSettings();
				notify("🗑️ Đã xoá toạ độ đã khoá");
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// 7. Nút [ ĐÓNG BẢNG (ESC / X) ]
			int btnCloseBottomY = boxY + boxH - 26;
			int btnCloseBottomH = 21;
			if (px >= boxX + 14 && px <= boxX + 14 + infoW && py >= btnCloseBottomY && py <= btnCloseBottomY + btnCloseBottomH)
			{
				activeInputIndex = -1;
				closeAllDialogs();
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// Click ra ngoài bảng -> Đóng
			if (px < boxX || px > boxX + boxW || py < boxY || py > boxY + boxH)
			{
				activeInputIndex = -1;
				closeAllDialogs();
			}

			GameCanvas.isPointerJustRelease = false;
		}
	}

	private static void handleMainDialogInput()
	{
		int boxW = 356;
		int boxH = 224;
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
				closeAllDialogs();
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// 2. Chuyển Tab (8 Tabs: Chiến Đấu | Săn Boss | Trong Khu | Tự Nhặt | Auto Item | Đồ Hoạ | FPS | Hệ Thống)
			int tabY = boxY + headerH + 3;
			int tabH = 21;
			int tabW = (boxW - 14) / 8; // ~42px mỗi tab
			if (py >= tabY && py <= tabY + tabH)
			{
				for (int t = 0; t < 8; t++)
				{
					int curTabX = boxX + 7 + t * tabW;
					if (px >= curTabX && px <= curTabX + tabW)
					{
						activeMainDialogTab = t;
						GameCanvas.isPointerJustRelease = false;
						return;
					}
				}
			}

			int contentY = tabY + tabH + 4;

			// 3. Xử lý click theo từng Tab
			if (activeMainDialogTab == 0) // ⚔ CHIẾN ĐẤU
			{
				int rowH = 17;
				int startY = contentY + 4;

				// Checkbox Tàn sát
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY && py < startY + rowH)
				{
					isTanSat = !isTanSat;
					notify("Tàn sát: " + (isTanSat ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Checkbox Đứng im pem toàn map
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY + rowH && py < startY + rowH * 2)
				{
					isStandStill = !isStandStill;
					notify("Đứng im pem toàn map: " + (isStandStill ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Checkbox Chỉ pem quái chỉ định
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY + rowH * 2 && py < startY + rowH * 3)
				{
					isFilterMob = !isFilterMob;
					notify("Chỉ pem quái chỉ định: " + (isFilterMob ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Checkbox Auto đổi khu
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY + rowH * 3 && py < startY + rowH * 4)
				{
					isAutoChangeZone = !isAutoChangeZone;
					notify("Đổi khu khi hết quái: " + (isAutoChangeZone ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Checkbox Tự động luyện tập TDLT (Phím T)
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY + rowH * 4 && py < startY + rowH * 5)
				{
					toggleAutoTrain();
					GameCanvas.isPointerJustRelease = false;
					return;
				}

				// 4 Nút cài đặt: [ 🎯 Quái Pem ] & [ ⚡ Cài Skill ] & [ 🔄 Goback ] & [ 🗺️ Xmap ]
				int btnActionY = startY + rowH * 5 + 8;
				int gap = 6;
				int btnW = (boxW - 32 - gap * 3) / 4;
				int btnH = 22;

				int btnMobX = boxX + 16;
				if (px >= btnMobX && px <= btnMobX + btnW && py >= btnActionY && py <= btnActionY + btnH)
				{
					showMobDialog();
					GameCanvas.isPointerJustRelease = false;
					return;
				}

				int btnSkillX = btnMobX + btnW + gap;
				if (px >= btnSkillX && px <= btnSkillX + btnW && py >= btnActionY && py <= btnActionY + btnH)
				{
					showSelectSkillMenu();
					GameCanvas.isPointerJustRelease = false;
					return;
				}

				int btnGobackX = btnSkillX + btnW + gap;
				if (px >= btnGobackX && px <= btnGobackX + btnW && py >= btnActionY && py <= btnActionY + btnH)
				{
					showGobackDialog();
					GameCanvas.isPointerJustRelease = false;
					return;
				}

				int btnXmapX = btnGobackX + btnW + gap;
				if (px >= btnXmapX && px <= btnXmapX + btnW && py >= btnActionY && py <= btnActionY + btnH)
				{
					closeAllDialogs();
					XmapManager.showXmapMenu();
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}
			else if (activeMainDialogTab == 1) // 👑 SĂN BOSS (TITLE RIÊNG THÔNG BÁO BOSS)
			{
				int startY = contentY + 6;
				int btnW = boxW - 32;
				int btnH = 22;
				int btnY = startY + 92;
				int btnX = boxX + 16;

				// Checkbox Thông báo Boss (tb)
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY && py < startY + 20)
				{
					BossManager.ToggleBossNotice();
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Nút to Bật/Tắt
				if (px >= btnX && px <= btnX + btnW && py >= btnY && py <= btnY + btnH)
				{
					BossManager.ToggleBossNotice();
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}
			else if (activeMainDialogTab == 2) // 👥 TRONG KHU (TITLE RIÊNG NGƯỜI & BOSS TRONG KHU)
			{
				int startY = contentY + 6;
				int btnW = boxW - 32;
				int btnH = 22;
				int btnY = startY + 92;
				int btnX = boxX + 16;

				// Checkbox Hiện người trong khu (nk)
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY && py < startY + 20)
				{
					BossManager.ToggleCharInMap();
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Nút to Bật/Tắt
				if (px >= btnX && px <= btnX + btnW && py >= btnY && py <= btnY + btnH)
				{
					BossManager.ToggleCharInMap();
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}
			else if (activeMainDialogTab == 3) // 📦 TỰ NHẶT & VỨT ĐỒ
			{
				int rowH = 18;
				int startY = contentY + 4;

				// Checkbox Tự động nhặt
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY && py < startY + rowH)
				{
					isAutoPick = !isAutoPick;
					notify("Tự động nhặt: " + (isAutoPick ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Checkbox Bùa thu hút
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY + rowH && py < startY + rowH * 2)
				{
					isBuaThuHut = !isBuaThuHut;
					notify("Bùa thu hút: " + (isBuaThuHut ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Checkbox Nhặt của người khác
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY + rowH * 2 && py < startY + rowH * 3)
				{
					isPickOthers = !isPickOthers;
					notify("Nhặt của người khác: " + (isPickOthers ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Checkbox Nhặt toàn map
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY + rowH * 3 && py < startY + rowH * 4)
				{
					isPickAllMap = !isPickAllMap;
					notify("Nhặt toàn map: " + (isPickAllMap ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}
				// Checkbox Tự động vứt rác
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY + rowH * 4 && py < startY + rowH * 5)
				{
					isAutoDrop = !isAutoDrop;
					notify("Tự động vứt rác: " + (isAutoDrop ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}

				// Nút Cài Đặt Nhặt & Vứt Chi Tiết
				int btnPickW = boxW - 32;
				int btnPickH = 20;
				int btnPickY = startY + rowH * 5 + 2;
				int btnPickX = boxX + 16;
				if (px >= btnPickX && px <= btnPickX + btnPickW && py >= btnPickY && py <= btnPickY + btnPickH)
				{
					isShowMainDialog = false;
					showAutoPickMenu();
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}
			else if (activeMainDialogTab == 4) // 🍗 AUTO ITEM
			{
				int startY = contentY + 8;

				// Checkbox Auto Item
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY && py < startY + 22)
				{
					isAutoItem = !isAutoItem;
					notify("Auto Item: " + (isAutoItem ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}

				// Nút Mở Bảng Cài Auto Item 2 Cột
				int btnItemW = boxW - 32;
				int btnItemH = 22;
				int btnItemY = startY + 68;
				int btnItemX = boxX + 16;
				if (px >= btnItemX && px <= btnItemX + btnItemW && py >= btnItemY && py <= btnItemY + btnItemH)
				{
					isShowMainDialog = false;
					showAutoItemMenu();
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}
			else if (activeMainDialogTab == 5) // 🎨 CÀI ĐẶT ĐỒ HOẠ (THẤP / TRUNG BÌNH / CAO)
			{
				int startY = contentY + 6;
				int btnH = 22;
				int btnW = (boxW - 32 - 12) / 3;
				int btnY = startY + 16;

				if (py >= btnY && py <= btnY + btnH)
				{
					for (int i = 0; i < 3; i++)
					{
						int bx = boxX + 16 + i * (btnW + 6);
						if (px >= bx && px <= bx + btnW)
						{
							GraphicsOptimizer.ApplyGraphicsLevel(i);
							GameCanvas.isPointerJustRelease = false;
							return;
						}
					}
				}
			}
			else if (activeMainDialogTab == 6) // ⚡ CÀI ĐẶT FPS (60, 90, 120, 144)
			{
				int startY = contentY + 6;
				int btnH = 22;
				int btnW = (boxW - 32 - 18) / 4;
				int btnY = startY + 16;
				int[] fpsList = new int[] { 60, 90, 120, 144 };

				// 4 Nút chọn FPS
				if (py >= btnY && py <= btnY + btnH)
				{
					for (int i = 0; i < 4; i++)
					{
						int bx = boxX + 16 + i * (btnW + 6);
						if (px >= bx && px <= bx + btnW)
						{
							GraphicsOptimizer.ApplyTargetFps(fpsList[i]);
							GameCanvas.isPointerJustRelease = false;
							return;
						}
					}
				}

				// Checkbox 1: Tự bóp FPS thông minh
				int chkY1 = btnY + btnH + 8;
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= chkY1 && py <= chkY1 + 16)
				{
					GraphicsOptimizer.ToggleDynamicFps();
					GameCanvas.isPointerJustRelease = false;
					return;
				}

				// Checkbox 2: Giảm xuống 30 FPS khi tab ra ngoài
				int chkY2 = chkY1 + 17;
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= chkY2 && py <= chkY2 + 16)
				{
					GraphicsOptimizer.ToggleThrottleBackgroundFps();
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}
			else if (activeMainDialogTab == 7) // ⚙ HỆ THỐNG & TỐC ĐỘ GAME
			{
				int startY = contentY + 8;

				// Checkbox Hiện HUD
				if (px >= boxX + 16 && px <= boxX + boxW - 16 && py >= startY && py < startY + 20)
				{
					isShowHUD = !isShowHUD;
					notify("Bảng HUD: " + (isShowHUD ? "BẬT" : "TẮT"));
					GameCanvas.isPointerJustRelease = false;
					return;
				}

				// Tiêu đề & nút tốc độ
				int spdTitleY = startY + 24;
				int btnMiniW = 24;
				int btnMiniH = 18;
				int btnMinusX = boxX + 225;
				int btnPlusX = boxX + 260;

				// Nút [-]
				if (px >= btnMinusX && px <= btnMinusX + btnMiniW && py >= spdTitleY && py <= spdTitleY + btnMiniH)
				{
					setSpeed(Mathf.Max(1f, currentSpeed - 1f));
					GameCanvas.isPointerJustRelease = false;
					return;
				}

				// Nút [+]
				if (px >= btnPlusX && px <= btnPlusX + btnMiniW && py >= spdTitleY && py <= spdTitleY + btnMiniH)
				{
					setSpeed(Mathf.Min(10f, currentSpeed + 1f));
					GameCanvas.isPointerJustRelease = false;
					return;
				}

				// 2 Hàng nút tốc độ (1x -> 10x)
				float[] speeds = new float[] { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f };
				int btnSpdW = (boxW - 32 - 16) / 5;
				int btnSpdH = 18;
				int gap = 4;
				int startBtnSpdX = boxX + 16;
				int btnSpdY1 = spdTitleY + 24;
				int btnSpdY2 = btnSpdY1 + btnSpdH + 4;

				// Hàng 1 (1x -> 5x)
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

				// Hàng 2 (6x -> 10x)
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
			}

			// 4. Nút [ ĐÓNG BẢNG (ESC / X) ] dưới đáy bảng
			int btnCloseBottomW = 160;
			int btnCloseBottomH = 19;
			int btnCloseBottomX = boxX + (boxW - btnCloseBottomW) / 2;
			int btnCloseBottomY = boxY + boxH - 22;
			if (px >= btnCloseBottomX && px <= btnCloseBottomX + btnCloseBottomW && py >= btnCloseBottomY && py <= btnCloseBottomY + btnCloseBottomH)
			{
				closeAllDialogs();
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// 5. Click ra ngoài bảng -> Đóng bảng
			if (px < boxX || px > boxX + boxW || py < boxY || py > boxY + boxH)
			{
				closeAllDialogs();
				GameCanvas.isPointerJustRelease = false;
				return;
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

			// Click ô nhập D/S ItemID Tự Dùng hoặc nút [Sửa]
			int inputEatY = startY + maxVisibleRows * rowH + 4;
			int btnEditW = 32;
			int tfEatX = boxX + 118;
			int tfEatW = boxW - 124 - btnEditW - 6;
			int tfEatH = 18;
			int btnEatEditX = tfEatX + tfEatW + 4;

			if (px >= btnEatEditX && px <= btnEatEditX + btnEditW && py >= inputEatY - 2 && py <= inputEatY + tfEatH + 2)
			{
				GameCanvas.inputDlg.show("Nhập D/S ItemID Tự Dùng (cách nhau dấu phẩy):", new Command("Lưu", ModManager.gI(), 10004, null), TField.INPUT_TYPE_ANY);
				if (GameCanvas.inputDlg.tfInput != null) GameCanvas.inputDlg.tfInput.setText(eatItemFilter);
				GameCanvas.isPointerJustRelease = false;
				return;
			}

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

	// Kiểm tra click vào nút HUD Menu & Nhặt trên màn hình (Đã gỡ nút trên màn hình, thay hoàn toàn bằng phím tắt X và N)
	public static bool checkHUDButtonClick()
	{
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
		isShowMobDialog = false;
		isShowGobackDialog = false;
		isShowMainDialog = true;
	}

	public static void showSelectSkillMenu()
	{
		isShowMainDialog = false;
		isShowItemDialog = false;
		isShowPickDialog = false;
		isShowMobDialog = false;
		isShowGobackDialog = false;
		scrollSkillIndex = 0;
		isShowSkillDialog = true;
	}

	public static void showAutoItemMenu()
	{
		isShowMainDialog = false;
		isShowSkillDialog = false;
		isShowPickDialog = false;
		isShowMobDialog = false;
		isShowGobackDialog = false;
		scrollItemIndex = 0;
		isShowItemDialog = true;
	}

	public static void showAutoPickMenu()
	{
		isShowMainDialog = false;
		isShowSkillDialog = false;
		isShowItemDialog = false;
		isShowMobDialog = false;
		isShowGobackDialog = false;
		isShowPickDialog = true;
	}

	public static void showMobDialog()
	{
		isShowMainDialog = false;
		isShowSkillDialog = false;
		isShowItemDialog = false;
		isShowPickDialog = false;
		isShowGobackDialog = false;
		scrollMobIndex = 0;
		isShowMobDialog = true;
	}

	public static void showGobackDialog()
	{
		isShowMainDialog = false;
		isShowSkillDialog = false;
		isShowItemDialog = false;
		isShowPickDialog = false;
		isShowMobDialog = false;
		isShowGobackDialog = true;

		// Đồng bộ toạ độ hiển thị trong ô nhập:
		// Nếu ĐÃ KHOÁ toạ độ trước đó: hiển thị toạ độ đang khoá
		// Nếu CHƯA KHOÁ toạ độ: đề xuất toạ độ hiện tại của nhân vật để không bao giờ bị mâu thuẫn!
		if (XmapManager.hasSavedGobackPos && XmapManager.gobackMapId != -1)
		{
			gobackPosText = XmapManager.gobackMapId + ", " + XmapManager.gobackZoneId + ", " + XmapManager.gobackX + ", " + XmapManager.gobackY;
		}
		else
		{
			Char cur = Char.myCharz();
			int cx = (cur != null) ? cur.cx : 0;
			int cy = (cur != null) ? cur.cy : 0;
			gobackPosText = TileMap.mapID + ", " + TileMap.zoneID + ", " + cx + ", " + cy;
			XmapManager.gobackMapId = TileMap.mapID;
			XmapManager.gobackZoneId = TileMap.zoneID;
			XmapManager.gobackX = cx;
			XmapManager.gobackY = cy;
		}
	}

	public void showMainMenu()
	{
		showMainControlDialog();
	}

	public void perform(int idAction, object p)
	{
		if (XmapManager.onMenuAction(idAction))
		{
			return;
		}

		if (idAction == 10001) // Lưu D/S ItemID Nhặt
		{
			if (GameCanvas.inputDlg != null && GameCanvas.inputDlg.tfInput != null)
			{
				pickItemFilter = GameCanvas.inputDlg.tfInput.getText();
			}
			saveSettings();
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
			saveSettings();
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
			clearDropBlacklist();
			saveSettings();
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
		else if (idAction == 10010) // Mở theo số lượng
		{
			if (GameCanvas.menu != null && GameCanvas.menu.showMenu)
			{
				GameCanvas.menu.doCloseMenu();
			}
			int defaultQty = (autoOpenPendingItem != null && autoOpenPendingItem.quantity > 0) ? autoOpenPendingItem.quantity : 1;
			GameCanvas.inputDlg.show("Nhập số lượng " + autoOpenItemName + " cần mở:", new Command("Bắt đầu", this, 10012, null), TField.INPUT_TYPE_NUMERIC);
			if (GameCanvas.inputDlg != null && GameCanvas.inputDlg.tfInput != null)
			{
				GameCanvas.inputDlg.tfInput.setText(defaultQty.ToString());
			}
		}
		else if (idAction == 10011) // Mở theo mili giây
		{
			if (GameCanvas.menu != null && GameCanvas.menu.showMenu)
			{
				GameCanvas.menu.doCloseMenu();
			}
			GameCanvas.inputDlg.show("Nhập khoảng cách mở (mili giây):", new Command("Bắt đầu", this, 10013, null), TField.INPUT_TYPE_NUMERIC);
			if (GameCanvas.inputDlg != null && GameCanvas.inputDlg.tfInput != null)
			{
				GameCanvas.inputDlg.tfInput.setText(autoOpenIntervalMs.ToString());
			}
		}
		else if (idAction == 10012) // Xác nhận mở theo số lượng
		{
			int count = 1;
			if (GameCanvas.inputDlg != null && GameCanvas.inputDlg.tfInput != null)
			{
				int.TryParse(GameCanvas.inputDlg.tfInput.getText().Trim(), out count);
			}
			if (count <= 0)
			{
				count = (autoOpenPendingItem != null && autoOpenPendingItem.quantity > 0) ? autoOpenPendingItem.quantity : 1;
			}
			GameCanvas.endDlg();
			startAutoOpen(0, count, 200L);
		}
		else if (idAction == 10013) // Xác nhận mở theo mili giây
		{
			long ms = 200L;
			if (GameCanvas.inputDlg != null && GameCanvas.inputDlg.tfInput != null)
			{
				long.TryParse(GameCanvas.inputDlg.tfInput.getText().Trim(), out ms);
			}
			if (ms <= 0L)
			{
				ms = 200L;
			}
			GameCanvas.endDlg();
			startAutoOpen(1, -1, ms);
		}
		else if (idAction == 10014) // Dừng / Đóng
		{
			if (GameCanvas.menu != null && GameCanvas.menu.showMenu)
			{
				GameCanvas.menu.doCloseMenu();
			}
			stopAutoOpenItem("Đã dừng tự động mở.");
		}
		else if (idAction == 20000)
		{
			SafeAccountInfo acc = p as SafeAccountInfo;
			if (acc != null)
			{
				loginWithAccount(acc);
			}
		}
		else if (idAction == 20001)
		{
			showAccountSelectorMenu();
		}
	}

	public void showAutoOpenMenu(Item item)
	{
		if (item == null || item.template == null)
		{
			return;
		}

		autoOpenPendingItem = item;
		autoOpenTemplateId = item.template.id;
		autoOpenItemName = item.template.name;

		MyVector menuItems = new MyVector();
		menuItems.addElement(new Command("Mở theo số lượng", this, 10010, item));
		menuItems.addElement(new Command("Mở theo mili giây", this, 10011, item));
		menuItems.addElement(new Command(mResources.CLOSE, this, 10014, null));

		GameCanvas.menu.startAt(menuItems, 3);
	}

	public static void startAutoOpen(int mode, int targetCount, long intervalMs)
	{
		autoOpenMode = mode;
		autoOpenTargetCount = targetCount;
		autoOpenRemainingCount = targetCount;
		autoOpenCountOpened = 0;
		autoOpenIntervalMs = (long)Mathf.Max(50f, (float)intervalMs);
		isAutoOpeningItem = true;
		lastAutoOpenTime = 0L;
		autoOpenWaitUntil = 0L;

		if (GameCanvas.panel != null && GameCanvas.panel.isShow)
		{
			GameCanvas.panel.hide();
		}
		if (GameCanvas.menu != null && GameCanvas.menu.showMenu)
		{
			GameCanvas.menu.doCloseMenu();
		}

		if (mode == 0)
		{
			notify("Bắt đầu mở " + targetCount + " " + autoOpenItemName);
		}
		else
		{
			notify("Bắt đầu mở " + autoOpenItemName + " (Khoảng cách: " + autoOpenIntervalMs + "ms)");
		}
	}

	public static void stopAutoOpenItem(string reason)
	{
		if (!isAutoOpeningItem)
		{
			return;
		}
		isAutoOpeningItem = false;
		autoOpenPendingItem = null;
		autoOpenTemplateId = -1;
		if (!string.IsNullOrEmpty(reason))
		{
			notify(reason);
		}
	}

	public static void updateAutoOpenItem(long now)
	{
		if (!isAutoOpeningItem)
		{
			return;
		}

		Char myChar = Char.myCharz();
		if (myChar == null || myChar.arrItemBag == null || myChar.statusMe == 14 || Char.isLoadingMap || Char.ischangingMap)
		{
			return;
		}

		// Nếu server yêu cầu chờ (cooldown/rate limit)
		if (now < autoOpenWaitUntil)
		{
			return;
		}

		// Kiểm tra hành trang đầy định kỳ (mỗi 500ms)
		if (now - lastAutoOpenBagFullCheckTime >= 500L)
		{
			lastAutoOpenBagFullCheckTime = now;
			if (GameScr.gI() != null && GameScr.gI().isBagFull())
			{
				bool hasEmptySlot = false;
				for (int s = 0; s < myChar.arrItemBag.Length; s++)
				{
					if (myChar.arrItemBag[s] == null)
					{
						hasEmptySlot = true;
						break;
					}
				}
				if (!hasEmptySlot)
				{
					stopAutoOpenItem("Hành trang đã đầy, dừng tự động mở!");
					return;
				}
			}
		}

		// Tự động nhặt các vật phẩm rớt trên map của bản thân hoặc trùng ID
		if (GameScr.vItemMap != null)
		{
			for (int j = 0; j < GameScr.vItemMap.size(); j++)
			{
				ItemMap im = (ItemMap)GameScr.vItemMap.elementAt(j);
				if (im != null && (im.template != null && im.template.id == autoOpenTemplateId || im.isAuraItem() || im.playerId == -2 || im.playerId == myChar.charID))
				{
					Service.gI().pickItem(im.itemMapID);
				}
			}
		}

		// Tính khoảng cách giữa 2 lần mở co giãn theo tốc độ game cài đặt trong menu
		float speed = Mathf.Max(1f, currentSpeed);
		long scaledDelay = (long)Mathf.Clamp((float)autoOpenIntervalMs / speed, 50f, 5000f);

		if (now - lastAutoOpenTime < scaledDelay)
		{
			return;
		}

		// Tìm vật phẩm trong hành trang
		int bagIdx = -1;
		Item itemToOpen = null;
		for (int i = 0; i < myChar.arrItemBag.Length; i++)
		{
			Item it = myChar.arrItemBag[i];
			if (it != null && it.template != null && it.template.id == autoOpenTemplateId)
			{
				bagIdx = i;
				itemToOpen = it;
				break;
			}
		}

		// Nếu không còn vật phẩm đó trong hành trang
		if (bagIdx == -1 || itemToOpen == null)
		{
			stopAutoOpenItem("Đã mở hết " + autoOpenItemName + "! (Tổng cộng: " + autoOpenCountOpened + ")");
			return;
		}

		// Gửi gói tin mở vật phẩm
		Service.gI().useItem(0, 1, (sbyte)bagIdx, -1);
		lastAutoOpenTime = now;
		autoOpenCountOpened++;

		// Nếu mở theo số lượng
		if (autoOpenMode == 0 && autoOpenTargetCount > 0)
		{
			autoOpenRemainingCount--;
			if (autoOpenRemainingCount <= 0)
			{
				stopAutoOpenItem("Đã hoàn thành mở " + autoOpenTargetCount + " " + autoOpenItemName + "!");
				return;
			}
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


	public static void loadSettings()
	{
		try
		{
			// Mặc định khi vào game: TẤT CẢ các toggle Auto Nhặt / Vứt / Nhặt Toàn Map ĐỀU TẮT
			isAutoPick = false;
			isPickOthers = false;
			isPickAllMap = false;
			isAutoDrop = false;

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

			string gbPos = Rms.loadRMSString("mod_goback_pos");
			if (!string.IsNullOrEmpty(gbPos))
			{
				gobackPosText = gbPos;
				applyGobackPosFromText();
			}

			string gbOn = Rms.loadRMSString("mod_is_goback");
			if (gbOn != null) XmapManager.isGoback = (gbOn == "1");

			string gbKi = Rms.loadRMSString("mod_is_goback_ki");
			if (gbKi != null) XmapManager.isGobackKi = (gbKi == "1");

			string gbLock = Rms.loadRMSString("mod_has_saved_pos");
			if (gbLock != null) XmapManager.hasSavedGobackPos = (gbLock == "1");

			if (!XmapManager.hasSavedGobackPos)
			{
				XmapManager.gobackMapId = -1;
				XmapManager.gobackZoneId = -1;
				XmapManager.gobackX = -1;
				XmapManager.gobackY = -1;
				gobackPosText = "";
			}
		}
		catch (Exception)
		{
		}
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

			Rms.saveRMSString("mod_goback_pos", gobackPosText ?? "");
			Rms.saveRMSString("mod_is_goback", XmapManager.isGoback ? "1" : "0");
			Rms.saveRMSString("mod_is_goback_ki", XmapManager.isGobackKi ? "1" : "0");
			Rms.saveRMSString("mod_has_saved_pos", XmapManager.hasSavedGobackPos ? "1" : "0");
		}
		catch (Exception)
		{
		}
	}

	// =========================================================================
	// QUẢN LÝ TÀI KHOẢN (QLTK) - ĐỌC accounts_safe.txt VÀ TỰ ĐỘNG ĐĂNG NHẬP
	// =========================================================================

	public static List<SafeAccountInfo> loadSafeAccounts()
	{
		List<SafeAccountInfo> list = new List<SafeAccountInfo>();
		string baseDir = AppDomain.CurrentDomain.BaseDirectory;
		string[] possiblePaths = new string[]
		{
			System.IO.Path.Combine(System.IO.Path.Combine(baseDir, "Data"), "accounts_safe.txt"),
			System.IO.Path.Combine(System.IO.Path.Combine(baseDir, ".."), System.IO.Path.Combine("Data", "accounts_safe.txt")),
			"Data/accounts_safe.txt",
			"../Data/accounts_safe.txt"
		};
		string foundPath = null;
		foreach (string p in possiblePaths)
		{
			if (System.IO.File.Exists(p))
			{
				foundPath = p;
				break;
			}
		}
		if (foundPath != null)
		{
			try
			{
				string[] lines = System.IO.File.ReadAllLines(foundPath);
				foreach (string line in lines)
				{
					if (string.IsNullOrEmpty(line) || !line.Contains("|")) continue;
					string[] parts = line.Split('|');
					if (parts.Length >= 3)
					{
						int sid = 1;
						int.TryParse(parts[1], out sid);
						string pass = decodeBase64Safe(parts[2]);
						list.Add(new SafeAccountInfo
						{
							Username = parts[0],
							ServerId = sid > 0 ? sid : 1,
							Password = pass,
							Note = parts.Length > 3 ? parts[3] : ""
						});
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("loadSafeAccounts error: " + ex);
			}
		}
		return list;
	}

	public static string decodeBase64Safe(string encoded)
	{
		if (string.IsNullOrEmpty(encoded)) return "";
		try
		{
			byte[] b = Convert.FromBase64String(encoded);
			return System.Text.Encoding.UTF8.GetString(b);
		}
		catch
		{
			return encoded;
		}
	}

	public static string decodeHexPayload(string hex)
	{
		if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0) return null;
		try
		{
			byte[] bytes = new byte[hex.Length / 2];
			for (int i = 0; i < hex.Length; i += 2)
			{
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			}
			return System.Text.Encoding.UTF8.GetString(bytes);
		}
		catch
		{
			return null;
		}
	}

	public static void initFromCommandLine()
	{
		try
		{
			string[] args = Environment.GetCommandLineArgs();
			if (args != null && args.Length >= 2)
			{
				int w = 0, h = 0;
				for (int i = 1; i < args.Length; i++)
				{
					string arg = args[i].Trim();

					// Tìm phân giải màn hình
					int val;
					if (int.TryParse(arg, out val))
					{
						if (w == 0 && val >= 300) w = val;
						else if (h == 0 && val >= 200) h = val;
					}

					// Tìm payload tài khoản dạng HEX
					if (arg.Length >= 6 && !arg.StartsWith("-"))
					{
						string decoded = decodeHexPayload(arg);
						if (!string.IsNullOrEmpty(decoded) && decoded.Contains("|"))
						{
							string[] parts = decoded.Split('|');
							if (parts.Length >= 3)
							{
								string user = parts[0];
								int sid = 1;
								int.TryParse(parts[1], out sid);
								string pass = parts[2];

								if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
								{
									autoLoginUser = user;
									autoLoginPass = pass;
									autoLoginServerIndex = (sid > 0) ? (sid - 1) : 0;
									isAutoLoginFromQLTK = true;
									autoLoginState = 0;
									autoLoginTimer = mSystem.currentTimeMillis();

									Rms.saveRMSString(Rms.RMS_acc, user);
									Rms.saveRMSString(Rms.RMS_pass, pass);
									Rms.saveRMSInt(ServerListScreen.RMS_svselect, autoLoginServerIndex);
								}
							}
						}
					}
				}

				if (w >= 300 && h >= 200)
				{
					Screen.SetResolution(w, h, fullscreen: false);
					isCustomResolutionSet = true;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("initFromCommandLine error: " + ex);
		}
	}

	public static void showAccountSelectorMenu()
	{
		try
		{
			List<SafeAccountInfo> accs = loadSafeAccounts();
			if (accs == null || accs.Count == 0)
			{
				GameCanvas.startOKDlg("Chưa tìm thấy file Data/accounts_safe.txt hoặc chưa có tài khoản nào!\nHãy mở tool QLTK_DragonBoy.exe để thêm tài khoản.");
				return;
			}

			MyVector menuItems = new MyVector();
			for (int i = 0; i < accs.Count; i++)
			{
				SafeAccountInfo acc = accs[i];
				string title = string.Format("[S{0}] {1}{2}", 
					acc.ServerId, 
					acc.Username, 
					string.IsNullOrEmpty(acc.Note) ? "" : " (" + acc.Note + ")");
				menuItems.addElement(new Command(title, ModManager.gI(), 20000, acc));
			}
			menuItems.addElement(new Command("Làm mới danh sách", ModManager.gI(), 20001, null));
			GameCanvas.menu.startAt(menuItems, 0);
		}
		catch (Exception ex)
		{
			Debug.LogError("showAccountSelectorMenu error: " + ex);
		}
	}

	public static void loginWithAccount(SafeAccountInfo acc)
	{
		if (acc == null) return;
		try
		{
			Rms.saveRMSString(Rms.RMS_acc, acc.Username);
			Rms.saveRMSString(Rms.RMS_pass, acc.Password);
			int svIndex = acc.ServerId - 1;
			if (svIndex < 0) svIndex = 0;
			if (ServerListScreen.nameServer != null && svIndex >= ServerListScreen.nameServer.Length)
			{
				svIndex = ServerListScreen.nameServer.Length - 1;
			}
			ServerListScreen.ipSelect = svIndex;
			Rms.saveRMSInt(ServerListScreen.RMS_svselect, svIndex);

			autoLoginUser = acc.Username;
			autoLoginPass = acc.Password;
			autoLoginServerIndex = svIndex;
			isAutoLoginFromQLTK = true;
			autoLoginState = 0;
			autoLoginTimer = mSystem.currentTimeMillis();

			if (Session_ME.gI().isConnected())
			{
				Session_ME.gI().close();
			}

			if (GameCanvas.serverScreen == null)
			{
				GameCanvas.serverScreen = new ServerListScreen();
			}
			GameCanvas.serverScreen.switchToMe();
			GameCanvas.startWaitDlg("Đang kết nối Server " + acc.ServerId + "...");
		}
		catch (Exception ex)
		{
			Debug.LogError("loginWithAccount error: " + ex);
			GameCanvas.startOKDlg("Lỗi đăng nhập: " + ex.Message);
		}
	}
}

public class SafeAccountInfo
{
	public string Username;
	public string Password;
	public int ServerId;
	public string Note;
}

