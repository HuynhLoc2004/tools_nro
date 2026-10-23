using System.Collections.Generic;

public class XmapController : IActionListener
{
	private const int TIME_DELAY_NEXTMAP = 200;
	private const int TIME_DELAY_RENEXTMAP = 500;
	private const int ID_ITEM_CAPSULE_VIP = 194;
	private const int ID_ITEM_CAPSULE = 193;
	private const int ID_ICON_ITEM_TDLT = 4387;

	private static readonly XmapController _Instance = new XmapController();

	public static XmapController Instance => _Instance;

	public static int IdMapEnd;
	public static List<int> WayXmap;
	public static int IndexWay;
	private static bool IsNextMapFailed;
	private static bool IsWait;
	private static long TimeStartWait;
	private static long TimeWait;
	private static bool IsWaitNextMap;
	public static bool IsXmapRunning => Pk9rXmap.IsXmapRunning;

	private static long stepStartTime = 0L;
	private static int stepLastMapId = -1;
	private static int lastVisitedTrunksMap = -1;

	public static void Update()
	{
		if (IsWaiting())
			return;

		if (XmapData.Instance().IsLoading)
			return;

		long now = mSystem.currentTimeMillis();

		// Giám sát tiến trình chuyển map: nếu bị kẹt ở cùng 1 map quá 6.5 giây (do server chờ / cooldown / lỗi)
		// -> Tự động huỷ trạng thái, đóng các bảng/popup kẹt và thông báo để người chơi bấm Z chọn lại
		if (TileMap.mapID != stepLastMapId)
		{
			stepLastMapId = TileMap.mapID;
			stepStartTime = now;
		}
		else
		{
			if (stepStartTime > 0L && now - stepStartTime > 6500L)
			{
				FinishXmap();
				GameCanvas.endDlg();
				InfoDlg.hide();
				GameCanvas.panel.hide();
				Char.isLockKey = false;
				Char.ischangingMap = false;
				GameScr.info1.addInfo("Xmap quá thời gian chờ, đã huỷ trạng thái. Vui lòng bấm Z chọn lại!", 0);
				return;
			}
		}

		if (IsWaitNextMap)
		{
			Wait(TIME_DELAY_NEXTMAP);
			IsWaitNextMap = false;
			return;
		}

		if (IsNextMapFailed)
		{
			XmapData.Instance().MyLinkMaps = null;
			WayXmap = null;
			IsNextMapFailed = false;
			return;
		}

		if (WayXmap == null)
		{
			if (XmapData.Instance().MyLinkMaps == null)
			{
				XmapData.Instance().LoadLinkMaps();
				return;
			}
			WayXmap = XmapAlgorithm.FindWay(TileMap.mapID, IdMapEnd);
			IndexWay = 0;
			if (WayXmap == null)
			{
				GameScr.info1.addInfo("Không thể tìm thấy đường đi", 0);
				FinishXmap();
				return;
			}
		}

		if (TileMap.mapID == WayXmap[WayXmap.Count - 1] && !XmapData.IsMyCharDie())
		{
			GameScr.info1.addInfo("Đã đến: " + (TileMap.mapNames != null && IdMapEnd < TileMap.mapNames.Length ? TileMap.mapNames[IdMapEnd] : ("Map " + IdMapEnd)), 0);
			XmapManager.onGobackArrived();
			FinishXmap();
			return;
		}

		if (IndexWay < WayXmap.Count && TileMap.mapID == WayXmap[IndexWay])
		{
			if (XmapData.IsMyCharDie())
			{
				Service.gI().returnTownFromDead();
				IsWaitNextMap = IsNextMapFailed = true;
			}
			else if (XmapData.CanNextMap())
			{
				if (IndexWay + 1 < WayXmap.Count)
				{
					NextMap(WayXmap[IndexWay + 1]);
					IsWaitNextMap = true;
				}
			}
			Wait(TIME_DELAY_RENEXTMAP);
			return;
		}

		if (IndexWay + 1 < WayXmap.Count && TileMap.mapID == WayXmap[IndexWay + 1])
		{
			IndexWay++;
			return;
		}

		IsNextMapFailed = true;
	}

	public void perform(int idAction, object p)
	{
		switch (idAction)
		{
			case 1:
				List<int> idMaps = (List<int>)p;
				ShowPanelXmap(idMaps);
				break;
		}
	}

	private static void Wait(int time)
	{
		IsWait = true;
		TimeStartWait = mSystem.currentTimeMillis();
		TimeWait = time;
	}

	private static bool IsWaiting()
	{
		if (IsWait && mSystem.currentTimeMillis() - TimeStartWait >= TimeWait)
			IsWait = false;
		return IsWait;
	}

	#region Thao tác của xmap
	public static void ShowXmapMenu()
	{
		XmapData.Instance().LoadGroupMapsFromFile("TextData\\GroupMapsXmap.txt");
		MyVector myVector = new MyVector();
		foreach (var groupMap in XmapData.Instance().GroupMaps)
			myVector.addElement(new Command(groupMap.NameGroup, _Instance, 1, groupMap.IdMaps));
		GameCanvas.menu.startAt(myVector, 3);
	}

	public static void ShowPanelXmap(List<int> idMaps)
	{
		Pk9rXmap.IsMapTransAsXmap = true;
		int len = idMaps.Count;
		GameCanvas.panel.mapNames = new string[len];
		GameCanvas.panel.planetNames = new string[len];
		for (int i = 0; i < len; i++)
		{
			string nameMap = (TileMap.mapNames != null && idMaps[i] < TileMap.mapNames.Length) ? TileMap.mapNames[idMaps[i]] : ("Map " + idMaps[i]);
			GameCanvas.panel.mapNames[i] = idMaps[i] + ": " + nameMap;
			GameCanvas.panel.planetNames[i] = "Xmap dekevn";
		}
		GameCanvas.panel.setTypeMapTrans();
		GameCanvas.panel.show();
	}

	public static void StartRunToMapId(int idMap)
	{
		if (TileMap.mapID == idMap)
		{
			GameScr.info1.addInfo("Đang ở map này rồi!", 0);
			FinishXmap();
			return;
		}

		IdMapEnd = idMap;
		WayXmap = null;
		IndexWay = 0;
		IsNextMapFailed = false;
		IsWait = false;
		IsWaitNextMap = false;
		Pk9rXmap.IsXmapRunning = true;
		XmapData.Instance().MyLinkMaps = null;
		stepStartTime = mSystem.currentTimeMillis();
		stepLastMapId = TileMap.mapID;
		lastVisitedTrunksMap = -1;

		// Nếu người dùng chủ động chạy Xmap đến một map khác:
		if (idMap != XmapManager.gobackMapId && (!XmapManager.isReturningHomeForKi || idMap != XmapManager.getHomeMapId()))
		{
			XmapManager.isGobacking = false;
		}

		string targetName = (TileMap.mapNames != null && idMap >= 0 && idMap < TileMap.mapNames.Length) 
			? TileMap.mapNames[idMap] 
			: ("Map " + idMap);
		GameScr.info1.addInfo("Xmap -> " + targetName, 0);
	}

	public static void FinishXmap()
	{
		Pk9rXmap.IsXmapRunning = false;
		IsNextMapFailed = false;
		IsWait = false;
		IsWaitNextMap = false;
		WayXmap = null;
		IndexWay = 0;
		XmapData.Instance().MyLinkMaps = null;
		stepStartTime = 0L;
		stepLastMapId = -1;
		lastVisitedTrunksMap = -1;
		InfoDlg.hide();
		Char.isLockKey = false;
		Char.ischangingMap = false;
	}

	public static void SaveIdMapCapsuleReturn()
	{
		Pk9rXmap.IdMapCapsuleReturn = TileMap.mapID;
	}

	private static void NextMap(int idMapNext)
	{
		List<MapNext> mapNexts = XmapData.Instance().GetMapNexts(TileMap.mapID);
		if (mapNexts != null)
		{
			foreach (MapNext mapNext in mapNexts)
			{
				if (mapNext.MapID == idMapNext)
				{
					NextMap(mapNext);
					return;
				}
			}
		}
		GameScr.info1.addInfo("Lỗi tại dữ liệu", 0);
	}

	private static void NextMap(MapNext mapNext)
	{
		switch (mapNext.Type)
		{
			case TypeMapNext.AutoWaypoint:
				NextMapAutoWaypoint(mapNext);
				break;
			case TypeMapNext.NpcMenu:
				NextMapNpcMenu(mapNext);
				break;
			case TypeMapNext.NpcPanel:
				NextMapNpcPanel(mapNext);
				break;
			case TypeMapNext.Position:
				NextMapPosition(mapNext);
				break;
			case TypeMapNext.Capsule:
				NextMapCapsule(mapNext);
				break;
		}
	}

	private static void NextMapAutoWaypoint(MapNext mapNext)
	{
		Waypoint waypoint = XmapData.FindWaypoint(mapNext.MapID);
		if (waypoint != null)
		{
			ModManager.teleportToWaypoint(waypoint);
		}
		else
		{
			int reqDir = XmapManager.getRequiredDirection(TileMap.mapID, mapNext.MapID);
			Waypoint dirWp = XmapManager.getWaypointByDirection(reqDir);
			if (dirWp != null)
			{
				ModManager.teleportToWaypoint(dirWp);
			}
			else if (TileMap.vGo != null && TileMap.vGo.size() > 0)
			{
				ModManager.teleportToWaypoint((Waypoint)TileMap.vGo.elementAt(0));
			}
		}
	}

	private static void teleportToNpc(int idNpc)
	{
		try
		{
			if (GameScr.vNpc != null)
			{
				for (int i = 0; i < GameScr.vNpc.size(); i++)
				{
					Npc npc = (Npc)GameScr.vNpc.elementAt(i);
					if (npc != null && (npc.template.npcTemplateId == idNpc || npc.charID == idNpc))
					{
						MoveMyChar(npc.cx, npc.cy - 5);
						return;
					}
				}
			}
		}
		catch { }
	}

	public static Npc FindTrunksInCurrentMap()
	{
		if (GameScr.vNpc != null)
		{
			for (int i = 0; i < GameScr.vNpc.size(); i++)
			{
				Npc npc = (Npc)GameScr.vNpc.elementAt(i);
				if (npc != null)
				{
					if (npc.template != null && (npc.template.npcTemplateId == 38 || (npc.template.name != null && npc.template.name.ToLower().Contains("trunk"))))
					{
						return npc;
					}
					if (npc.charID == 38 || npc.npcId == 38)
					{
						return npc;
					}
				}
			}
		}
		return null;
	}

	public static bool IsTrunksMap(int mapId)
	{
		return mapId == 27 || mapId == 28 || mapId == 29;
	}

	public static bool IsFutureMap(int mapId)
	{
		return mapId == 102 || (mapId >= 92 && mapId <= 100) || mapId == 103;
	}

	private static void NextMapNpcMenu(MapNext mapNext)
	{
		int idNpc = mapNext.Info[0];
		if (idNpc == 38) // NPC Trunks chuyển qua lại Tương Lai
		{
			HandleTrunksTransition(mapNext);
			return;
		}

		teleportToNpc(idNpc);
		Service.gI().openMenu(idNpc);
		for (int i = 1; i < mapNext.Info.Length; i++)
		{
			int select = mapNext.Info[i];
			Service.gI().confirmMenu((short)idNpc, (sbyte)select);
		}
	}

	private static void HandleTrunksTransition(MapNext mapNext)
	{
		Npc trunks = FindTrunksInCurrentMap();
		if (trunks != null)
		{
			// ĐÃ TÌM THẤY TRUNKS TẠI MAP HIỆN TẠI!
			GameScr.info1.addInfo("Đã gặp Trunks tại " + TileMap.mapName + "! Đang qua Tương Lai...", 0);
			teleportToNpc(trunks.template != null ? trunks.template.npcTemplateId : 38);
			int npcId = trunks.template != null ? trunks.template.npcTemplateId : 38;
			Service.gI().openMenu(npcId);
			for (int i = 1; i < mapNext.Info.Length; i++)
			{
				int select = mapNext.Info[i];
				Service.gI().confirmMenu((short)npcId, (sbyte)select);
			}
			Wait(1500);
			return;
		}

		// NẾU Ở TƯƠNG LAI (map 102) mà chưa thấy Trunks:
		if (TileMap.mapID == 102)
		{
			GameScr.info1.addInfo("Đang chờ Trunks xuất hiện...", 0);
			Wait(1000);
			return;
		}

		// NẾU Ở TRÁI ĐẤT (map 27, 28, 29) VÀ KHÔNG THẤY TRUNKS TRONG MAP HIỆN TẠI:
		// Tự động dịch chuyển đi tìm Trunks qua 3 map (Rừng Bambo - 27, Rừng Dương Xỉ - 28, Nam Kamê - 29)
		if (IsTrunksMap(TileMap.mapID))
		{
			int currentM = TileMap.mapID;
			int nextSearchMap = 28;

			if (currentM == 27)
			{
				nextSearchMap = 28;
				lastVisitedTrunksMap = 27;
			}
			else if (currentM == 29)
			{
				nextSearchMap = 28;
				lastVisitedTrunksMap = 29;
			}
			else if (currentM == 28)
			{
				if (lastVisitedTrunksMap == 27)
				{
					nextSearchMap = 29;
				}
				else if (lastVisitedTrunksMap == 29)
				{
					nextSearchMap = 27;
				}
				else
				{
					nextSearchMap = 27;
				}
				lastVisitedTrunksMap = 28;
			}

			string nextMapName = (nextSearchMap == 27) ? "Rừng Bambo" : ((nextSearchMap == 28) ? "Rừng Dương Xỉ" : "Nam Kamê");
			GameScr.info1.addInfo("Trunks không ở " + TileMap.mapName + ", đang tìm ở " + nextMapName + "...", 0);

			Waypoint wp = XmapData.FindWaypoint(nextSearchMap);
			if (wp != null)
			{
				ModManager.teleportToWaypoint(wp);
			}
			else
			{
				int reqDir = XmapManager.getRequiredDirection(currentM, nextSearchMap);
				Waypoint dirWp = XmapManager.getWaypointByDirection(reqDir);
				if (dirWp != null)
				{
					ModManager.teleportToWaypoint(dirWp);
				}
			}

			Wait(1200);
			return;
		}

		// Nếu chưa tới 27, 28, 29: Di chuyển tới map 28 trước
		Waypoint wpTo28 = XmapData.FindWaypoint(28);
		if (wpTo28 != null)
		{
			ModManager.teleportToWaypoint(wpTo28);
		}
		Wait(1000);
	}

	private static void NextMapNpcPanel(MapNext mapNext)
	{
		int idNpc = mapNext.Info[0];
		int selectMenu = mapNext.Info[1];
		int selectPanel = mapNext.Info[2];
		teleportToNpc(idNpc);
		Service.gI().openMenu(idNpc);
		Service.gI().confirmMenu((short)idNpc, (sbyte)selectMenu);
		Service.gI().requestMapSelect(selectPanel);
	}

	private static void NextMapPosition(MapNext mapNext)
	{
		int xPos = mapNext.Info[0];
		int yPos = mapNext.Info[1];
		MoveMyChar(xPos, yPos);
		Service.gI().requestChangeMap();
		Service.gI().getMapOffline();
	}

	private static void prepareCharacterForCapsule()
	{
		Char myChar = Char.myCharz();
		if (myChar != null)
		{
			myChar.skillPaint = null;
			myChar.dart = null;
			myChar.isCharge = false;
			myChar.isAttack = false;
		}
		ModManager.pauseAttackUntil = mSystem.currentTimeMillis() + 500L;
		Char.isLockKey = false;
		Char.ischangingMap = false;
		InfoDlg.hide();
	}

	private static void NextMapCapsule(MapNext mapNext)
	{
		SaveIdMapCapsuleReturn();
		int index = mapNext.Info[0];
		prepareCharacterForCapsule();
		Service.gI().requestMapSelect(index);
	}
	#endregion

	#region Thao tác với game
	private static int GetItemBagIndex(int templateId, out short actualTemplateId)
	{
		actualTemplateId = (short)templateId;
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.arrItemBag == null) return -1;
		for (int i = 0; i < myChar.arrItemBag.Length; i++)
		{
			Item item = myChar.arrItemBag[i];
			if (item != null && item.template != null)
			{
				int id = item.template.id;
				string name = (item.template.name != null) ? item.template.name.ToLower() : "";
				if (templateId == ID_ITEM_CAPSULE_VIP)
				{
					if (id == ID_ITEM_CAPSULE_VIP || (name.Contains("capsule") && (name.Contains("đặc biệt") || name.Contains("vip"))))
					{
						actualTemplateId = item.template.id;
						return i;
					}
				}
				else if (templateId == ID_ITEM_CAPSULE)
				{
					if (id == ID_ITEM_CAPSULE || (id >= 390 && id <= 393) || (id >= 381 && id <= 385) || name.Contains("capsule") || name.Contains("capsu") || name.Contains("bay"))
					{
						actualTemplateId = item.template.id;
						return i;
					}
				}
				else if (id == templateId)
				{
					actualTemplateId = item.template.id;
					return i;
				}
			}
		}
		return -1;
	}

	public static void UseCapsuleNormal()
	{
		prepareCharacterForCapsule();
		Pk9rXmap.IsShowPanelMapTrans = false;
		XmapData.Instance().StartWaitCapsule();
		int bagIdx = GetItemBagIndex(ID_ITEM_CAPSULE, out short actualTemplateId);
		if (bagIdx != -1)
		{
			Service.gI().useItem(0, 1, (sbyte)bagIdx, actualTemplateId);
		}
		else
		{
			Service.gI().useItem(0, 1, -1, actualTemplateId);
		}
	}

	public static void UseCapsuleVip()
	{
		prepareCharacterForCapsule();
		Pk9rXmap.IsShowPanelMapTrans = false;
		XmapData.Instance().StartWaitCapsule();
		int bagIdx = GetItemBagIndex(ID_ITEM_CAPSULE_VIP, out short actualTemplateId);
		if (bagIdx != -1)
		{
			Service.gI().useItem(0, 1, (sbyte)bagIdx, actualTemplateId);
		}
		else
		{
			Service.gI().useItem(0, 1, -1, actualTemplateId);
		}
	}
	
	public static void HideInfoDlg()
	{
		InfoDlg.hide();
	}

	public static void MoveMyChar(int x, int y)
	{
		Char myChar = Char.myCharz();
		if (myChar == null) return;
		myChar.cx = x;
		myChar.cy = y;
		ModManager.sendDirectMovePacket(x, y);
		Service.isForceSendMove = true;
		Service.gI().charMove();

		if (ItemTime.isExistItem(ID_ICON_ITEM_TDLT) || ModManager.isAutoTrainBuffActive())
			return;

		myChar.cx = x;
		myChar.cy = y + 1;
		Service.isForceSendMove = true;
		Service.gI().charMove();
		myChar.cx = x;
		myChar.cy = y;
		Service.isForceSendMove = true;
		Service.gI().charMove();
	}

	public static void RequestChangeMap(Waypoint waypoint)
	{
		if (waypoint.isOffline || TileMap.isTrainingMap())
		{
			Service.gI().getMapOffline();
			return;
		}
		if (waypoint.isEnter)
		{
			try { waypoint.perform(2, null); } catch { }
		}
		Service.gI().requestChangeMap();
	}
	#endregion
}
