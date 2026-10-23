using System;

public class Pk9rXmap
{
	public static bool IsXmapRunning = false;
	public static bool IsMapTransAsXmap = false;
	public static bool IsShowPanelMapTrans = true;
	public static bool IsUseCapsuleNormal = false;
	public static bool IsUseCapsuleVip = true;
	public static int IdMapCapsuleReturn = -1;

	public static bool Chat(string text)
	{
		if (string.IsNullOrEmpty(text))
			return false;

		text = text.Trim();

		if (text == "xmp")
		{
			if (IsXmapRunning)
			{
				XmapController.FinishXmap();
				GameScr.info1.addInfo("Đã huỷ Xmap", 0);
			}
			else
			{
				XmapController.ShowXmapMenu();
			}
		}
		else if (IsGetInfoChat<int>(text, "xmp"))
		{
			if (IsXmapRunning)
			{
				XmapController.FinishXmap();
				GameScr.info1.addInfo("Đã huỷ Xmap", 0);
			}
			else
			{
				int idMap = GetInfoChat<int>(text, "xmp");
				XmapController.StartRunToMapId(idMap);
			}
		}
		else if (text == "csb")
		{
			IsUseCapsuleNormal = !IsUseCapsuleNormal;
			GameScr.info1.addInfo("Sử dụng capsule thường Xmap: " + (IsUseCapsuleNormal ? "Bật" : "Tắt"), 0);
		}
		else if (text == "csdb")
		{
			IsUseCapsuleVip = !IsUseCapsuleVip;
			GameScr.info1.addInfo("Sử dụng capsule đặc biệt Xmap: " + (IsUseCapsuleVip ? "Bật" : "Tắt"), 0);
		}
		else
		{
			return false;
		}
		return true;
	}

	public static bool HotKeys()
	{
		switch (GameCanvas.keyAsciiPress)
		{
			case 'x':
				Chat("xmp");
				break;
			case 'c':
				Chat("csb");
				break;
			default:
				return false;
		}
		return true;
	}

	public static void Update()
	{
		if (XmapData.Instance().IsLoading)
			XmapData.Instance().Update();
		if (IsXmapRunning)
			XmapController.Update();
	}

	public static void Info(string text)
	{
		if (string.IsNullOrEmpty(text))
			return;

		if (text.Contains("chưa thể đến khu vực") || text.Contains("Không thể thực hiện") || text.Contains("Vui lòng chờ") || text.Contains("thử lại sau") || text.Contains("Từ từ"))
		{
			if (IsXmapRunning)
			{
				XmapController.FinishXmap();
				GameCanvas.endDlg();
				InfoDlg.hide();
				GameCanvas.panel.hide();
				Char.isLockKey = false;
				Char.ischangingMap = false;
				GameScr.info1.addInfo("Xmap bị gián đoạn: " + text + ". Đã huỷ trạng thái để bạn bấm Z chọn lại!", 0);
			}
		}
	}

	public static bool XoaTauBay(object obj)
	{
		Teleport teleport = (Teleport)obj;
		if (teleport != null && teleport.isMe)
		{
			Char.myCharz().isTeleport = false;
			if (teleport.type == 0)
			{
				Controller.isStopReadMessage = false;
				Char.ischangingMap = true;
			}
			Teleport.vTeleport.removeElement(teleport);
			return true;
		}
		return false;
	}

	public static void SelectMapTrans(int selected)
	{
		if (IsMapTransAsXmap)
		{
			IsMapTransAsXmap = false;
			XmapController.HideInfoDlg();
			if (GameCanvas.panel.mapNames != null && selected >= 0 && selected < GameCanvas.panel.mapNames.Length)
			{
				string mapName = GameCanvas.panel.mapNames[selected];
				int idMap = GetIdMapFromPanelXmap(mapName);
				GameCanvas.panel.hide();
				XmapController.StartRunToMapId(idMap);
			}
			return;
		}
		// Người dùng tự chọn map bay qua Capsule
		XmapManager.isGobacking = false;
		XmapController.SaveIdMapCapsuleReturn();
		Service.gI().requestMapSelect(selected);
	}

	public static int GetIdMapFromPanelXmap(string mapName)
	{
		try
		{
			int colonIdx = mapName.IndexOf(':');
			if (colonIdx > 0)
			{
				string numStr = mapName.Substring(0, colonIdx).Trim();
				if (int.TryParse(numStr, out int idM))
					return idM;
			}
		}
		catch
		{
		}
		return XmapData.GetIdMapFromName(mapName);
	}

	public static void ShowPanelMapTrans()
	{
		IsMapTransAsXmap = false;

		// Nếu Xmap đang chạy scan capsule ngầm trong background (IsShowPanelMapTrans == false):
		if (!IsShowPanelMapTrans)
		{
			IsShowPanelMapTrans = true;
			return;
		}

		// Người dùng tự mở Capsule (từ túi đồ / phím tắt / NPC) -> LUÔN hiển thị bình thường để người dùng tự chọn!
		GameCanvas.panel.setTypeMapTrans();
		GameCanvas.panel.show();
	}

	public static void FixBlackScreen()
	{
		Controller.gI().loadCurrMap(0);
		Service.gI().finishLoadMap();
		Char.isLoadingMap = false;
	}

	#region Không cần liên kết với game
	private static bool IsGetInfoChat<T>(string text, string s)
	{
		if (!text.StartsWith(s))
		{
			return false;
		}
		try
		{
			Convert.ChangeType(text.Substring(s.Length).Trim(), typeof(T));
		}
		catch
		{
			return false;
		}
		return true;
	}

	private static T GetInfoChat<T>(string text, string s)
	{
		return (T)Convert.ChangeType(text.Substring(s.Length).Trim(), typeof(T));
	}
	#endregion
}
