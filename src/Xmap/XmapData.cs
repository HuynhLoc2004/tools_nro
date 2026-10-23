using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class XmapData
{
	private const int ID_MAP_HOME_BASE = 21;
	private const int ID_MAP_TTVT_BASE = 24;
	private const int ID_ITEM_CAPSULE_VIP = 194;
	private const int ID_ITEM_CAPSULE_NORMAL = 193;
	private const int ID_MAP_TPVGT = 19;
	private const int ID_MAP_TO_COLD = 109;

	public List<GroupMap> GroupMaps;
	public Dictionary<int, List<MapNext>> MyLinkMaps;
	public bool IsLoading;
	private bool IsLoadingCapsule;
	public static bool isReceivedCapsuleFromServer = false;

	private static readonly string DEFAULT_GROUP_MAPS = @"Trái đất
	42 21 0 1 2 3 4 5 6 27 28 29 30 47 24
Namec
	43 22 7 8 9 11 12 13 10 31 32 33 34 25
Xayda
	44 23 14 15 16 17 18 20 19 35 36 37 38 26 52 84
Nappa
	68 69 70 71 72 64 65 63 66 67 73 74 75 76 77 81 82 83 79 80 131 132 133
Tương lai 
	102 92 93 94 96 97 98 99 100 103
Cold
	109 108 107 110 106 105";

	private static readonly string DEFAULT_LINK_MAPS = @"# Trái Đất - Namec
	24 25 1 10 0
	25 24 1 11 0

# Trái Đất - Xayda
	24 26 1 10 1
	26 24 1 12 0

# Namec - Xayda
	25 26 1 11 1
	26 25 1 12 1

# Hành tinh -> Siêu thị
	24 84 1 10 2
	25 84 1 11 2
	26 84 1 12 2

# Tpvgt - Nappa
	19 68 1 12 1
	68 19 1 12 0

# Nappa -> Yadat
	80 131 1 60 0

# Trái Đât - Tương lai
	27 102 1 38 1
	28 102 1 38 1
	29 102 1 38 1
	102 24 1 38 1";

	private static readonly string DEFAULT_AUTO_LINK_MAPS = @"# Trái đất
	42 0 1 2 3 4 5 6
	3 27 28 29 30
	2 24
	1 47
	5 29

# Namec
	43 7 8 9 11 12 13 10
	11 31 32 33 34
	9 25
	13 33

# Xayda
	52 44 14 15 16 17 18 20 19
	17 35 36 37 38
	16 26
	20 37

# Nappa
	68 69 70 71 72 64 65 63 66 67 73 74 75 76 77 81 82 83 79 80

# Tương Lai
	102 92 93 94 96 97 98 99 100 103

# Cold
	109 108 107 110 106
	109 105
	109 106
	106 107
	108 105

# Yadat
	131 132 133

# Nappa - Cold
	80 105";

	private XmapData()
	{
		GroupMaps = new List<GroupMap>();
		MyLinkMaps = null;
		IsLoading = false;
		IsLoadingCapsule = false;
	}

	private static XmapData _Instance;

	public static XmapData Instance()
	{
		if (_Instance == null)
			_Instance = new XmapData();
		return _Instance;
	}

	private static long timeStartLoading = 0L;

	public void LoadLinkMaps()
	{
		IsLoading = true;
		timeStartLoading = mSystem.currentTimeMillis();
	}

	public void Update()
	{
		if (IsLoading && timeStartLoading > 0L && mSystem.currentTimeMillis() - timeStartLoading > 2000L)
		{
			IsLoadingCapsule = false;
			IsLoading = false;
			Pk9rXmap.IsShowPanelMapTrans = true;
			timeStartLoading = 0L;
			return;
		}

		if (!IsLoadingCapsule)
		{
			if (Char.isLoadingMap || Controller.isStopReadMessage)
			{
				if (timeStartLoading > 0L && mSystem.currentTimeMillis() - timeStartLoading > 1000L)
				{
					Controller.isStopReadMessage = false;
				}
				else
				{
					return;
				}
			}
			LoadLinkMapBase();
			if (CanUseCapsuleVip())
			{
				XmapController.UseCapsuleVip();
				IsLoadingCapsule = true;
				return;
			}
			if (CanUseCapsuleNormal())
			{
				XmapController.UseCapsuleNormal();
				IsLoadingCapsule = true;
				return;
			}
			IsLoading = false;
			return;
		}
		if (IsWaitInfoMapTrans())
			return;
		LoadLinkMapCapsule();
		IsLoadingCapsule = false;
		IsLoading = false;
	}

	public void LoadGroupMapsFromFile(string path)
	{
		GroupMaps.Clear();
		try
		{
			TextReader reader = null;
			if (File.Exists(path))
			{
				reader = new StreamReader(path);
			}
			else
			{
				reader = new StringReader(DEFAULT_GROUP_MAPS);
			}

			using (reader)
			{
				string textLine;
				while ((textLine = reader.ReadLine()) != null)
				{
					textLine = textLine.Trim();
					if (textLine.StartsWith("#") || textLine.Equals(string.Empty))
						continue;

					string textLine2 = reader.ReadLine();
					if (textLine2 == null) break;
					textLine2 = textLine2.Trim();

					string[] textData = textLine2.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
					List<int> data = new List<int>();
					for (int i = 0; i < textData.Length; i++)
					{
						if (int.TryParse(textData[i], out int idM))
						{
							data.Add(idM);
						}
					}

					GroupMaps.Add(new GroupMap(textLine, data));
				}
			}
		}
		catch (Exception e)
		{
			GameScr.info1.addInfo(e.Message, 0);
		}
		RemoveMapsHomeInGroupMaps();
	}

	private void RemoveMapsHomeInGroupMaps()
	{
		int cgender = Char.myCharz().cgender;
		foreach (var groupMap in GroupMaps)
		{
			switch (cgender)
			{
				case 0:
					groupMap.IdMaps.Remove(22);
					groupMap.IdMaps.Remove(23);
					break;
				case 1:
					groupMap.IdMaps.Remove(21);
					groupMap.IdMaps.Remove(23);
					break;
				default:
					groupMap.IdMaps.Remove(21);
					groupMap.IdMaps.Remove(22);
					break;
			}
		}
	}

	private void LoadLinkMapCapsule()
	{
		if (!isReceivedCapsuleFromServer)
		{
			return;
		}
		isReceivedCapsuleFromServer = false;

		AddKeyLinkMaps(TileMap.mapID);
		string[] mapNames = GameCanvas.panel.mapNames;
		if (mapNames == null) return;
		int idMap;
		for (int select = 0; select < mapNames.Length; select++)
		{
			idMap = GetIdMapFromName(mapNames[select]);
			if (idMap != -1)
			{
				int[] info = new int[] { select };
				MyLinkMaps[TileMap.mapID].Add(new MapNext(idMap, TypeMapNext.Capsule, info));
			}
		}
	}

	private void LoadLinkMapBase()
	{
		MyLinkMaps = new Dictionary<int, List<MapNext>>();
		LoadLinkMapsFromFile("TextData\\LinkMapsXmap.txt");
		LoadLinkMapsAutoWaypointFromFile("TextData\\AutoLinkMapsWaypoint.txt");
		LoadLinkMapsHome();
		LoadLinkMapSieuThi();
		LoadLinkMapToCold();
	}

	private void LoadLinkMapsFromFile(string path)
	{
		try
		{
			TextReader reader = null;
			if (File.Exists(path))
			{
				reader = new StreamReader(path);
			}
			else
			{
				reader = new StringReader(DEFAULT_LINK_MAPS);
			}

			using (reader)
			{
				string textLine;
				while ((textLine = reader.ReadLine()) != null)
				{
					textLine = textLine.Trim();

					if (textLine.StartsWith("#") || textLine.Equals(string.Empty))
						continue;

					string[] textData = textLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
					List<int> dataList = new List<int>();
					for (int i = 0; i < textData.Length; i++)
					{
						if (int.TryParse(textData[i], out int val))
						{
							dataList.Add(val);
						}
					}
					if (dataList.Count < 3) continue;

					int[] data = dataList.ToArray();
					int lenInfo = data.Length - 3;
					int[] info = new int[lenInfo];
					Array.Copy(data, 3, info, 0, lenInfo);

					LoadLinkMap(data[0], data[1], (TypeMapNext)data[2], info);
				}
			}
		}
		catch (Exception e)
		{
			GameScr.info1.addInfo(e.Message, 0);
		}
	}

	private void LoadLinkMapsAutoWaypointFromFile(string path)
	{
		try
		{
			TextReader reader = null;
			if (File.Exists(path))
			{
				reader = new StreamReader(path);
			}
			else
			{
				reader = new StringReader(DEFAULT_AUTO_LINK_MAPS);
			}

			using (reader)
			{
				string textLine;
				while ((textLine = reader.ReadLine()) != null)
				{
					textLine = textLine.Trim();

					if (textLine.StartsWith("#") || textLine.Equals(string.Empty))
						continue;

					string[] textData = textLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
					List<int> dataList = new List<int>();
					for (int i = 0; i < textData.Length; i++)
					{
						if (int.TryParse(textData[i], out int val))
						{
							dataList.Add(val);
						}
					}
					int[] data = dataList.ToArray();

					for (int i = 0; i < data.Length; i++)
					{
						if (i != 0)
							LoadLinkMap(data[i], data[i - 1], TypeMapNext.AutoWaypoint, null);

						if (i != data.Length - 1)
							LoadLinkMap(data[i], data[i + 1], TypeMapNext.AutoWaypoint, null);
					}
				}
			}
		}
		catch (Exception e)
		{
			GameScr.info1.addInfo(e.Message, 0);
		}
	}

	private void LoadLinkMapsHome()
	{
		const int ID_MAP_LANG_BASE = 7;
		int cgender = Char.myCharz().cgender;

		int idMapHome = ID_MAP_HOME_BASE + cgender;
		int idMapLang = ID_MAP_LANG_BASE * cgender;

		LoadLinkMap(idMapLang, idMapHome, TypeMapNext.AutoWaypoint, null);
		LoadLinkMap(idMapHome, idMapLang, TypeMapNext.AutoWaypoint, null);
	}

	private void LoadLinkMapSieuThi()
	{
		const int ID_MAP_TTVT_BASE = 24;
		const int ID_MAP_SIEU_THI = 84;
		const int ID_NPC = 10;
		const int INDEX = 0;

		int offset = Char.myCharz().cgender;
		int idMapNext = ID_MAP_TTVT_BASE + offset;
		int[] info = new int[]
		{
			ID_NPC, INDEX
		};
		LoadLinkMap(ID_MAP_SIEU_THI, idMapNext, TypeMapNext.NpcMenu, info);
	}

	private void LoadLinkMapToCold()
	{
		if (Char.myCharz().taskMaint == null || Char.myCharz().taskMaint.taskId <= 30)
			return;

		const int ID_NPC = 12;
		const int INDEX = 0;

		int[] info = new int[]
		{
			ID_NPC, INDEX
		};
		LoadLinkMap(ID_MAP_TPVGT, ID_MAP_TO_COLD, TypeMapNext.NpcMenu, info);
	}

	public List<MapNext> GetMapNexts(int idMap)
	{
		if (CanGetMapNexts(idMap))
			return MyLinkMaps[idMap];
		return null;
	}

	public bool CanGetMapNexts(int idMap)
	{
		return MyLinkMaps != null && MyLinkMaps.ContainsKey(idMap);
	}

	private void LoadLinkMap(int idMapStart, int idMapNext, TypeMapNext type, int[] info)
	{
		AddKeyLinkMaps(idMapStart);
		MapNext mapNext = new MapNext(idMapNext, type, info);
		MyLinkMaps[idMapStart].Add(mapNext);
	}

	private void AddKeyLinkMaps(int idMap)
	{
		if (!MyLinkMaps.ContainsKey(idMap))
			MyLinkMaps.Add(idMap, new List<MapNext>());
	}

	private static long timeStartWaitMapTrans = 0L;

	public void StartWaitCapsule()
	{
		timeStartWaitMapTrans = mSystem.currentTimeMillis();
	}

	private bool IsWaitInfoMapTrans()
	{
		if (Pk9rXmap.IsShowPanelMapTrans)
		{
			timeStartWaitMapTrans = 0L;
			return false;
		}
		if (timeStartWaitMapTrans > 0L && mSystem.currentTimeMillis() - timeStartWaitMapTrans > 1200L)
		{
			timeStartWaitMapTrans = 0L;
			Pk9rXmap.IsShowPanelMapTrans = true; // Unblock để không bị kẹt vĩnh viễn
			return false;
		}
		return true;
	}

	public static Waypoint FindWaypoint(int idMap)
	{
		Waypoint waypoint;
		string textPopup;
		if (TileMap.vGo == null) return null;
		for (int i = 0; i < TileMap.vGo.size(); i++)
		{
			waypoint = (Waypoint)TileMap.vGo.elementAt(i);
			textPopup = GetTextPopup(waypoint.popup);
			if (TileMap.mapNames != null && idMap >= 0 && idMap < TileMap.mapNames.Length && textPopup.Equals(TileMap.mapNames[idMap]))
			{
				return waypoint;
			}
		}
		return XmapManager.findWaypointLeadingTo(TileMap.mapID, idMap);
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

	public static bool IsMyCharDie()
	{
		return Char.myCharz().statusMe == 14 || Char.myCharz().cHP <= 0;
	}

	public static bool CanNextMap()
	{
		return !Char.isLoadingMap && !Char.ischangingMap && !Controller.isStopReadMessage;
	}

	public static int GetIdMapFromName(string mapName)
	{
		if (string.IsNullOrEmpty(mapName)) return -1;
		int offset = Char.myCharz().cgender;

		if (mapName.Equals("Về nhà") || mapName.Contains("Về nhà"))
			return ID_MAP_HOME_BASE + offset;

		if (mapName.Equals("Trạm tàu vũ trụ") || mapName.Contains("Trạm tàu vũ trụ"))
			return ID_MAP_TTVT_BASE + offset;

		if (mapName.Contains("Về chỗ cũ: ") || mapName.StartsWith("Về chỗ cũ"))
		{
			if (XmapManager.gobackMapId >= 0)
				return XmapManager.gobackMapId;
			if (Pk9rXmap.IdMapCapsuleReturn >= 0)
				return Pk9rXmap.IdMapCapsuleReturn;
			string oldMapName = mapName.Replace("Về chỗ cũ: ", "").Replace("Về chỗ cũ", "").Trim();
			mapName = oldMapName;
		}

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
		catch { }

		if (TileMap.mapNames != null)
		{
			for (int i = 0; i < TileMap.mapNames.Length; i++)
			{
				if (TileMap.mapNames[i] != null && mapName.Equals(TileMap.mapNames[i]))
					return i;
			}
		}

		string norm = XmapManager.removeAccents(mapName).ToLower().Trim();
		if (TileMap.mapNames != null)
		{
			for (int i = 0; i < TileMap.mapNames.Length; i++)
			{
				if (TileMap.mapNames[i] != null && XmapManager.removeAccents(TileMap.mapNames[i]).ToLower().Trim().Equals(norm))
					return i;
			}
		}

		foreach (var kvp in XmapManager.defaultMapNames)
		{
			if (XmapManager.removeAccents(kvp.Value).ToLower().Trim().Equals(norm))
				return kvp.Key;
		}

		return -1;
	}

	private static string GetTextPopup(PopUp popUp)
	{
		if (popUp == null || popUp.says == null) return string.Empty;
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < popUp.says.Length; i++)
		{
			stringBuilder.Append(popUp.says[i]);
			stringBuilder.Append(" ");
		}
		return stringBuilder.ToString().Trim();
	}

	private static bool CanUseCapsuleNormal()
	{
		return !IsMyCharDie() && (Pk9rXmap.IsUseCapsuleNormal || XmapManager.isGoback || XmapManager.hasSavedGobackPos) && HasItemCapsuleNormal();
	}

	private static bool HasItemCapsuleNormal()
	{
		Item[] items = Char.myCharz().arrItemBag;
		if (items == null) return false;
		for (int i = 0; i < items.Length; i++)
		{
			if (items[i] != null && items[i].template != null)
			{
				int id = items[i].template.id;
				string name = items[i].template.name != null ? items[i].template.name.ToLower() : "";
				if (id == ID_ITEM_CAPSULE_NORMAL || (id >= 390 && id <= 393) || (id >= 381 && id <= 385) || name.Contains("capsule") || name.Contains("capsu"))
					return true;
			}
		}
		return false;
	}

	private static bool CanUseCapsuleVip()
	{
		return !IsMyCharDie() && (Pk9rXmap.IsUseCapsuleVip || XmapManager.isGoback || XmapManager.hasSavedGobackPos) && HasItemCapsuleVip();
	}

	private static bool HasItemCapsuleVip()
	{
		Item[] items = Char.myCharz().arrItemBag;
		if (items == null) return false;
		for (int i = 0; i < items.Length; i++)
		{
			if (items[i] != null && items[i].template != null)
			{
				int id = items[i].template.id;
				string name = items[i].template.name != null ? items[i].template.name.ToLower() : "";
				if (id == ID_ITEM_CAPSULE_VIP || (name.Contains("capsule") && (name.Contains("đặc biệt") || name.Contains("vip"))))
					return true;
			}
		}
		return false;
	}
}
