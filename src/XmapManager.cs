using System;
using System.Collections.Generic;
using UnityEngine;

public class XmapManager
{
	private static XmapManager instance;
	private static long lastDialogClickTime = 0L;

	public static XmapManager gI()
	{
		if (instance == null)
		{
			instance = new XmapManager();
		}
		return instance;
	}

	// Trạng thái Xmap
	public static bool isActive = false;
	public static int targetMapId = -1;
	public static List<int> currentPath = new List<int>();
	public static int currentPathIndex = 0;
	public static long lastStepTime = 0L;
	private static int lastMapIdChecked = -1;
	private static int retryCount = 0;

	// Trạng thái Goback (Tự động quay lại khi chết / Hết KI)
	public static bool isGoback = false;
	public static bool isGobackKi = false;
	public static bool hasSavedGobackPos = false;
	public static bool isReturningHomeForKi = false;
	public static long pendingSyncGobackPosTime = 0L;
	public static int gobackMapId = -1;
	public static int gobackZoneId = -1;
	public static int gobackX = -1;
	public static int gobackY = -1;
	public static bool isGobacking = false;
	public static bool isAutoCapsuleUse = false;
	public static bool isManualTravel = false;
	public static bool wasTanSatActive = false;
	public static bool wasAutoTrainActive = false;
	private static long lastDieCheckTime = 0L;
	private static long lastKiCheckTime = 0L;
	private static long lastRespawnWaitTime = 0L;
	private static long lastZoneRequestTime = 0L;
	private static long lastXmapRequestTime = 0L;
	private static long lastPullPosTime = 0L;
	private static int lastMapIdTracked = -1;
	private static long lastMapChangeTime = 0L;

	// Cấu trúc đồ thị liên kết Map
	private static Dictionary<int, List<int>> mapGraph = new Dictionary<int, List<int>>();

	static XmapManager()
	{
		initMapGraph();
	}

	private static void addEdge(int u, int v)
	{
		if (!mapGraph.ContainsKey(u))
		{
			mapGraph[u] = new List<int>();
		}
		if (!mapGraph[u].Contains(v))
		{
			mapGraph[u].Add(v);
		}

		if (!mapGraph.ContainsKey(v))
		{
			mapGraph[v] = new List<int>();
		}
		if (!mapGraph[v].Contains(u))
		{
			mapGraph[v].Add(u);
		}
	}

	private static void initMapGraph()
	{
		mapGraph.Clear();

		// ==========================================
		// 1. TRÁI ĐẤT (CÁC CỔNG ĐI BỘ NỐI NHAU THỰC TẾ)
		// ==========================================
		addEdge(21, 0);   // Nhà Gohan <-> Làng Aru
		addEdge(0, 1);    // Làng Aru <-> Đồi hoa cúc
		addEdge(0, 24);   // Làng Aru <-> Trạm tàu Trái Đất (Cổng Trạm tàu vũ trụ ở Làng)
		addEdge(0, 42);   // Làng Aru <-> Vách núi Aru
		addEdge(1, 2);    // Đồi hoa cúc <-> Thung lũng tre
		addEdge(2, 3);    // Thung lũng tre <-> Rừng nấm
		addEdge(3, 4);    // Rừng nấm <-> Rừng xương
		addEdge(3, 27);   // Rừng nấm <-> Rừng bamboo
		addEdge(4, 5);    // Rừng xương <-> Đảo Kame
		addEdge(5, 6);    // Đảo Kame <-> Đông Karin
		addEdge(27, 28);  // Rừng bamboo <-> Rừng dương xỉ
		addEdge(28, 29);  // Rừng dương xỉ <-> Nam Kamê
		addEdge(29, 30);  // Nam Kamê <-> Đảo Bulma

		// Tháp Karin & Thánh Địa Kaio
		addEdge(45, 46);  // Chân tháp Karin <-> Đỉnh tháp Karin
		addEdge(46, 48);  // Đỉnh tháp Karin <-> Thần điện
		addEdge(48, 49);  // Thần điện <-> Thánh địa Kaio
		addEdge(49, 50);  // Thánh địa Kaio <-> Địa ngục

		// ==========================================
		// 2. NAMẾC (CÁC CỔNG ĐI BỘ NỐI NHAU THỰC TẾ)
		// ==========================================
		addEdge(22, 7);   // Nhà Moori <-> Làng Mori
		addEdge(7, 8);    // Làng Mori <-> Đồi nấm tím
		addEdge(7, 25);   // Làng Mori <-> Trạm tàu Namếc (Cổng Trạm tàu vũ trụ ở Làng)
		addEdge(7, 43);   // Làng Mori <-> Vách núi Moori
		addEdge(8, 9);    // Đồi nấm tím <-> Thị trấn Moori
		addEdge(9, 11);   // Thị trấn Moori <-> Thung lũng Mây
		addEdge(11, 12);  // Thung lũng Mây <-> Thung lũng đá
		addEdge(12, 13);  // Thung lũng đá <-> Đảo Guru
		addEdge(13, 10);  // Đảo Guru <-> Thung lũng Namếc
		addEdge(9, 31);   // Thị trấn Moori <-> Núi Ba Hòn
		addEdge(31, 32);  // Núi Ba Hòn <-> Suối nước trong
		addEdge(32, 33);  // Suối nước trong <-> Rừng tuyết Namếc
		addEdge(33, 34);  // Rừng tuyết Namếc <-> Lâu đài Guru

		// ==========================================
		// 3. XAYDA (CÁC CỔNG ĐI BỘ NỐI NHAU THỰC TẾ)
		// ==========================================
		addEdge(23, 14);  // Nhà Broly <-> Làng Kakarot
		addEdge(14, 15);  // Làng Kakarot <-> Đồi hoang
		addEdge(14, 26);  // Làng Kakarot <-> Trạm tàu Xayda (Cổng Trạm tàu vũ trụ ở Làng)
		addEdge(14, 44);  // Làng Kakarot <-> Vách núi Kakarot
		addEdge(15, 16);  // Đồi hoang <-> Làng Plant
		addEdge(16, 17);  // Làng Plant <-> Rừng nguyên sinh
		addEdge(17, 18);  // Rừng nguyên sinh <-> Rừng cọ
		addEdge(17, 35);  // Rừng nguyên sinh <-> Núi khỉ vàng
		addEdge(18, 20);  // Rừng cọ <-> Vách núi đen
		addEdge(20, 19);  // Vách núi đen <-> Thành phố Vegeta
		addEdge(35, 36);  // Núi khỉ vàng <-> Núi khỉ đen
		addEdge(36, 37);  // Núi khỉ đen <-> Hang quỷ chim
		addEdge(37, 38);  // Hang quỷ chim <-> Núi dây leo

		// ==========================================
		// LIÊN HÀNH TINH (Qua Trạm Tàu Vũ Trụ & Siêu Thị)
		// ==========================================
		addEdge(24, 25);  // Trạm tàu Trái Đất <-> Trạm tàu Namếc
		addEdge(24, 26);  // Trạm tàu Trái Đất <-> Trạm tàu Xayda
		addEdge(25, 26);  // Trạm tàu Namếc <-> Trạm tàu Xayda
		addEdge(24, 84);  // Trạm tàu TĐ <-> Siêu thị
		addEdge(25, 84);  // Trạm tàu Namếc <-> Siêu thị
		addEdge(26, 84);  // Trạm tàu Xayda <-> Siêu thị

		// ==========================================
		// 4. FIDE & NÚI TUYẾT (63 - 83)
		// ==========================================
		addEdge(63, 64);
		addEdge(64, 65);
		addEdge(65, 66);
		addEdge(66, 67);
		addEdge(63, 68);
		addEdge(68, 69);
		addEdge(69, 70);
		addEdge(70, 71);
		addEdge(71, 72);
		addEdge(72, 73);
		addEdge(73, 74);
		addEdge(74, 75);
		addEdge(75, 76);
		addEdge(76, 77);
		addEdge(77, 78); // Căn cứ Fide
		addEdge(68, 79);
		addEdge(79, 80);
		addEdge(80, 81);
		addEdge(81, 82);
		addEdge(82, 83); // Đỉnh núi tuyết

		// ==========================================
		// 5. TƯƠNG LAI (92 - 103)
		// ==========================================
		addEdge(92, 93);
		addEdge(93, 94);
		addEdge(94, 96);
		addEdge(96, 97);
		addEdge(97, 98);
		addEdge(98, 99);
		addEdge(99, 100);
		addEdge(100, 102);
		addEdge(102, 103); // Võ đài Xên bọ hung

		// ==========================================
		// 6. HÀNH TINH COLD (105 - 111)
		// ==========================================
		addEdge(105, 106);
		addEdge(106, 107);
		addEdge(107, 108);
		addEdge(108, 109);
		addEdge(109, 110);
		addEdge(110, 111);
	}

	// Thuật toán tìm đường ngắn nhất (BFS)
	public static List<int> findPath(int startMap, int destMap)
	{
		if (startMap == destMap)
		{
			return new List<int> { startMap };
		}

		Queue<int> queue = new Queue<int>();
		Dictionary<int, int> parent = new Dictionary<int, int>();
		HashSet<int> visited = new HashSet<int>();

		queue.Enqueue(startMap);
		visited.Add(startMap);

		bool found = false;

		while (queue.Count > 0)
		{
			int curr = queue.Dequeue();
			if (curr == destMap)
			{
				found = true;
				break;
			}

			if (mapGraph.ContainsKey(curr))
			{
				List<int> neighbors = mapGraph[curr];
				for (int i = 0; i < neighbors.Count; i++)
				{
					int next = neighbors[i];
					if (!visited.Contains(next))
					{
						visited.Add(next);
						parent[next] = curr;
						queue.Enqueue(next);
					}
				}
			}
		}

		if (!found)
		{
			return null;
		}

		List<int> path = new List<int>();
		int crawl = destMap;
		path.Add(crawl);
		while (parent.ContainsKey(crawl))
		{
			crawl = parent[crawl];
			path.Add(crawl);
		}
		path.Reverse();
		return path;
	}

	public static bool isWaitingCapsule = false;
	public static long capsuleWaitStartTime = 0L;

	public static string getPlanetNameOfMap(int mapId)
	{
		if ((mapId >= 0 && mapId <= 6) || mapId == 21 || mapId == 24 || (mapId >= 27 && mapId <= 30) || (mapId >= 42 && mapId <= 50))
		{
			return "trai dat";
		}
		if ((mapId >= 7 && mapId <= 13) || mapId == 22 || mapId == 25 || (mapId >= 31 && mapId <= 34) || mapId == 43)
		{
			return "namec";
		}
		if ((mapId >= 14 && mapId <= 20) || mapId == 23 || mapId == 26 || (mapId >= 35 && mapId <= 38) || mapId == 44)
		{
			return "xayda";
		}
		if (mapId >= 63 && mapId <= 83)
		{
			return "fide";
		}
		if (mapId >= 92 && mapId <= 103)
		{
			return "tuong lai";
		}
		if (mapId >= 105 && mapId <= 111)
		{
			return "cold";
		}
		return "";
	}

	public static int getCapsuleBagIndex()
	{
		Char myChar = Char.myCharz();
		if (myChar == null || myChar.arrItemBag == null) return -1;
		// Ưu tiên 1: Capsule đặc biệt (ID 194)
		for (int i = 0; i < myChar.arrItemBag.Length; i++)
		{
			Item it = myChar.arrItemBag[i];
			if (it != null && it.template != null && it.template.id == 194)
			{
				return (it.indexUI >= 0) ? it.indexUI : i;
			}
		}
		// Ưu tiên 2: Capsule thường (ID 193) hoặc capsule khác
		for (int i = 0; i < myChar.arrItemBag.Length; i++)
		{
			Item it = myChar.arrItemBag[i];
			if (it != null && it.template != null)
			{
				int id = it.template.id;
				string name = (it.template.name != null) ? it.template.name.ToLower() : "";
				if (id == 193 || (id >= 390 && id <= 393) || (id >= 381 && id <= 385) || name.Contains("capsule") || name.Contains("capsu"))
				{
					return (it.indexUI >= 0) ? it.indexUI : i;
				}
			}
		}
		return -1;
	}

	// Bắt đầu Xmap đến Map chỉ định
	public static void startXmap(int destMapId)
	{
		if (TileMap.mapID == destMapId)
		{
			onGobackArrived();
			return;
		}

		targetMapId = destMapId;
		isActive = true;

		// Khởi chạy hệ thống dẫn đường Xmap
		XmapController.StartRunToMapId(destMapId);
	}

	public static void stopXmap()
	{
		XmapController.FinishXmap();
		isActive = false;
		targetMapId = -1;
		isGobacking = false;
		if (GameCanvas.panel != null && GameCanvas.panel.isShow && GameCanvas.panel.type == 14)
		{
			GameCanvas.panel.hide();
		}
		ModManager.notify("Đã dừng Xmap");
	}

	public static readonly Dictionary<int, string> defaultMapNames = new Dictionary<int, string>()
	{
		{ 0, "Làng Aru" },
		{ 1, "Đồi hoa cúc" },
		{ 2, "Thung lũng tre" },
		{ 3, "Rừng nấm" },
		{ 4, "Rừng xương" },
		{ 5, "Đảo Kame" },
		{ 6, "Đông Karin" },
		{ 7, "Làng Mori" },
		{ 8, "Đồi nấm tím" },
		{ 9, "Thị trấn Moori" },
		{ 10, "Thung lũng Namếc" },
		{ 11, "Thung lũng Mây" },
		{ 12, "Thung lũng đá" },
		{ 13, "Đảo Guru" },
		{ 14, "Làng Kakarot" },
		{ 15, "Đồi hoang" },
		{ 16, "Làng Plant" },
		{ 17, "Rừng nguyên sinh" },
		{ 18, "Rừng cọ" },
		{ 19, "Thành phố Vegeta" },
		{ 20, "Vách núi đen" },
		{ 21, "Nhà Gohan" },
		{ 22, "Nhà Moori" },
		{ 23, "Nhà Broly" },
		{ 24, "Trạm tàu Trái Đất" },
		{ 25, "Trạm tàu Namếc" },
		{ 26, "Trạm tàu Xayda" },
		{ 27, "Rừng bamboo" },
		{ 28, "Rừng dương xỉ" },
		{ 29, "Nam Kamê" },
		{ 30, "Đảo Bulma" },
		{ 31, "Núi Ba Hòn" },
		{ 32, "Suối nước trong" },
		{ 33, "Rừng tuyết Namếc" },
		{ 34, "Lâu đài Guru" },
		{ 35, "Núi khỉ vàng" },
		{ 36, "Núi khỉ đen" },
		{ 37, "Hang quỷ chim" },
		{ 38, "Núi dây leo" },
		{ 42, "Vách núi Aru" },
		{ 43, "Vách núi Moori" },
		{ 44, "Vách núi Kakarot" },
		{ 45, "Chân tháp Karin" },
		{ 46, "Đỉnh tháp Karin" },
		{ 47, "Rừng cọ ban đêm" },
		{ 48, "Thần điện" },
		{ 49, "Thánh địa Kaio" },
		{ 50, "Địa ngục" },
		{ 51, "Đại hội võ thuật" },
		{ 52, "Đấu trường ĐHVT" },
		{ 53, "Trại độc nhãn 1" },
		{ 63, "Thung lũng Nappa" },
		{ 64, "Vực cấm" },
		{ 65, "Núi Appule" },
		{ 66, "Căn cứ Raspber" },
		{ 67, "Thung lũng Raspber" },
		{ 68, "Thung lũng Guldo" },
		{ 69, "Đồi Guldo" },
		{ 70, "Thung lũng Recome" },
		{ 71, "Đồi Recome" },
		{ 72, "Thung lũng Burter" },
		{ 73, "Đồi Burter" },
		{ 74, "Thung lũng Jeice" },
		{ 75, "Đồi Jeice" },
		{ 76, "Thung lũng Ginyu" },
		{ 77, "Đồi Ginyu" },
		{ 78, "Căn cứ Fide" },
		{ 79, "Rừng tuyết Fide" },
		{ 80, "Rừng cọ lạnh" },
		{ 81, "Đồi tuyết" },
		{ 82, "Núi tuyết Fide" },
		{ 83, "Đỉnh núi tuyết" },
		{ 84, "Siêu thị liên vũ trụ" },
		{ 92, "Thành phố Tương Lai" },
		{ 93, "Đồi hoa cúc TL" },
		{ 94, "Thung lũng tre TL" },
		{ 96, "Rừng nấm TL" },
		{ 97, "Rừng xương TL" },
		{ 98, "Rừng cọ TL" },
		{ 99, "Rừng bamboo TL" },
		{ 100, "Rừng dương xỉ TL" },
		{ 102, "Nhà Bulma TL" },
		{ 103, "Võ đài Xên bọ hung" },
		{ 105, "Cánh đồng tuyết Cold" },
		{ 106, "Rừng tuyết Cold" },
		{ 107, "Núi tuyết Cold" },
		{ 108, "Dòng sông băng" },
		{ 109, "Rừng băng" },
		{ 110, "Hang băng" },
		{ 111, "Đông Cold" },
		{ 112, "Đấu trường siêu cấp" },
		{ 113, "Hành lang ĐTSC" },
		{ 114, "Cửa ải 1 Mabư" },
		{ 115, "Cửa ải 2 Mabư" },
		{ 117, "Cửa ải 3 Mabư" },
		{ 118, "Cửa ải 4 Mabư" },
		{ 119, "Cửa ải 5 Mabư" },
		{ 120, "Phòng tập thời gian" },
		{ 127, "Cửa ải Mabư 14h" },
		{ 128, "Tầng 2 Mabư 14h" },
		{ 129, "Võ đài liên vũ trụ" },
		{ 130, "Lôi đài liên vũ trụ" },
		{ 131, "Hành tinh Yardrat 1" },
		{ 132, "Hành tinh Yardrat 2" },
		{ 133, "Hành tinh Yardrat 3" },
		{ 135, "Cửa hang kho báu" },
		{ 136, "Hang kho báu" },
		{ 137, "Động kho báu" },
		{ 138, "Kho báu hải tặc" },
		{ 141, "Đầu con đường rắn độc" },
		{ 142, "Thân con đường rắn độc" },
		{ 143, "Đuôi con đường rắn độc" },
		{ 144, "Điện Kaio" },
		{ 147, "Khí Gas Hủy Diệt 1" },
		{ 148, "Khí Gas Hủy Diệt 2" },
		{ 149, "Khí Gas Hủy Diệt 3" },
		{ 151, "Căn cứ Dr. Lychee" },
		{ 152, "Tàu Hatchiyack" }
	};

	public static string getMapName(int mapId)
	{
		if (TileMap.mapNames != null && mapId >= 0 && mapId < TileMap.mapNames.Length && !string.IsNullOrEmpty(TileMap.mapNames[mapId]))
		{
			return TileMap.mapNames[mapId];
		}
		if (defaultMapNames.ContainsKey(mapId))
		{
			return defaultMapNames[mapId];
		}
		return "Map " + mapId;
	}

	public static string removeAccents(string text)
	{
		if (string.IsNullOrEmpty(text)) return "";
		string s = text.Trim().ToLower();
		string[] patterns = new string[]
		{
			"aàáạảãâầấậẩẫăằắặẳẵ",
			"eèéẹẻẽêềếệểễ",
			"iìíịỉĩ",
			"oòóọỏõôồốộổỗơờớợởỡ",
			"uùúụủũưừứựửữ",
			"yỳýỵỷỹ",
			"dđ"
		};
		char[] replaces = new char[] { 'a', 'e', 'i', 'o', 'u', 'y', 'd' };

		for (int i = 0; i < patterns.Length; i++)
		{
			for (int j = 1; j < patterns[i].Length; j++)
			{
				s = s.Replace(patterns[i][j], replaces[i]);
			}
		}
		return s;
	}

	public static int findMapIdByName(string search)
	{
		if (string.IsNullOrEmpty(search)) return -1;
		search = removeAccents(search);

		// 1. So khớp ID số nguyên trực tiếp
		int idNum;
		if (int.TryParse(search, out idNum))
		{
			return idNum;
		}

		// 2. Tìm kiếm CHÍNH XÁC trong danh sách tên của Server
		if (TileMap.mapNames != null)
		{
			for (int i = 0; i < TileMap.mapNames.Length; i++)
			{
				if (!string.IsNullOrEmpty(TileMap.mapNames[i]))
				{
					string sName = removeAccents(TileMap.mapNames[i]);
					if (sName.Equals(search))
					{
						return i;
					}
				}
			}
		}

		// 3. Tìm kiếm CHÍNH XÁC trong từ điển mặc định
		foreach (KeyValuePair<int, string> kvp in defaultMapNames)
		{
			string dName = removeAccents(kvp.Value);
			if (dName.Equals(search))
			{
				return kvp.Key;
			}
		}

		// 4. Tìm kiếm GẦN ĐÚNG (chỉ khi từ khóa dài >= 4 ký tự)
		if (search.Length >= 4)
		{
			if (TileMap.mapNames != null)
			{
				for (int i = 0; i < TileMap.mapNames.Length; i++)
				{
					if (!string.IsNullOrEmpty(TileMap.mapNames[i]))
					{
						string sName = removeAccents(TileMap.mapNames[i]);
						if (sName.Contains(search))
						{
							return i;
						}
					}
				}
			}

			foreach (KeyValuePair<int, string> kvp in defaultMapNames)
			{
				string dName = removeAccents(kvp.Value);
				if (dName.Contains(search))
				{
					return kvp.Key;
				}
			}
		}

		return -1;
	}

	public static void learnCurrentMapWaypoints()
	{
		if (TileMap.vGo == null || TileMap.vGo.size() == 0 || TileMap.mapID == -1)
		{
			return;
		}

		for (int i = 0; i < TileMap.vGo.size(); i++)
		{
			Waypoint wp = (Waypoint)TileMap.vGo.elementAt(i);
			if (wp != null && wp.popup != null && wp.popup.says != null && wp.popup.says.Length > 0)
			{
				string say = wp.popup.says[0];
				int targetId = findMapIdByName(say);
				if (targetId != -1 && targetId != TileMap.mapID)
				{
					addEdge(TileMap.mapID, targetId);
				}
			}
		}
	}

	// Tìm Waypoint dẫn sang nextMapId chuẩn xác 100%
	public static Waypoint findWaypointLeadingTo(int currMap, int nextMapId)
	{
		if (TileMap.vGo == null || TileMap.vGo.size() == 0)
		{
			return null;
		}

		// Nếu map chỉ có 1 waypoint duy nhất thì chắc chắn là nó
		if (TileMap.vGo.size() == 1)
		{
			return (Waypoint)TileMap.vGo.elementAt(0);
		}

		// 1. Cổng Trạm tàu vũ trụ (Map 24, 25, 26, 84) từ làng (Map 0, 7, 14)
		if (nextMapId == 24 || nextMapId == 25 || nextMapId == 26 || nextMapId == 84)
		{
			// Quét theo tên hoặc popup says
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint wp = (Waypoint)TileMap.vGo.elementAt(i);
				if (wp != null)
				{
					string n = !string.IsNullOrEmpty(wp.name) ? removeAccents(wp.name) : "";
					if (n.Contains("tram tau") || n.Contains("tau vu tru"))
					{
						return wp;
					}
					if (wp.popup != null && wp.popup.says != null)
					{
						string s = removeAccents(string.Join(" ", wp.popup.says));
						if (s.Contains("tram tau") || s.Contains("tau vu tru"))
						{
							return wp;
						}
					}
				}
			}
			// Nếu không có tên, chọn cổng isEnter/isOffline ở khu vực trung tâm map (tránh nhà Gohan/Moori ở rìa)
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint wp = (Waypoint)TileMap.vGo.elementAt(i);
				if (wp != null && (wp.isEnter || wp.isOffline))
				{
					int wpMid = (wp.minX + wp.maxX) / 2;
					if (wpMid > 250 && wpMid < TileMap.pxw - 250)
					{
						return wp;
					}
				}
			}
		}

		// 2. Quay về Làng từ Trạm tàu (24 -> 0, 25 -> 7, 26 -> 14): Cổng phía bên trái
		if ((currMap == 24 && nextMapId == 0) || (currMap == 25 && nextMapId == 7) || (currMap == 26 && nextMapId == 14))
		{
			return getWaypointByDirection(0);
		}

		string targetNameRaw = getMapName(nextMapId);
		string targetNorm = removeAccents(targetNameRaw);

		// 3. So khớp CHÍNH XÁC với wp.name hoặc toàn bộ popup.says
		for (int i = 0; i < TileMap.vGo.size(); i++)
		{
			Waypoint wp = (Waypoint)TileMap.vGo.elementAt(i);
			if (wp != null)
			{
				if (!string.IsNullOrEmpty(wp.name))
				{
					string wpNorm = removeAccents(wp.name);
					if (wpNorm.Equals(targetNorm))
					{
						return wp;
					}
				}
				if (wp.popup != null && wp.popup.says != null && wp.popup.says.Length > 0)
				{
					string fullSay = removeAccents(string.Join(" ", wp.popup.says));
					if (fullSay.Equals(targetNorm))
					{
						return wp;
					}
				}
			}
		}

		// 4. So khớp CHỨA NHAU (Contains) nếu chuỗi đủ dài (>= 4 ký tự)
		for (int i = 0; i < TileMap.vGo.size(); i++)
		{
			Waypoint wp = (Waypoint)TileMap.vGo.elementAt(i);
			if (wp != null)
			{
				if (!string.IsNullOrEmpty(wp.name))
				{
					string wpNorm = removeAccents(wp.name);
					if (wpNorm.Length >= 4 && targetNorm.Length >= 4 && (wpNorm.Contains(targetNorm) || targetNorm.Contains(wpNorm)))
					{
						return wp;
					}
				}
				if (wp.popup != null && wp.popup.says != null && wp.popup.says.Length > 0)
				{
					string fullSay = removeAccents(string.Join(" ", wp.popup.says));
					if (fullSay.Length >= 4 && targetNorm.Length >= 4 && (fullSay.Contains(targetNorm) || targetNorm.Contains(fullSay)))
					{
						return wp;
					}
				}
			}
		}

		// 5. BẢNG QUY TẮC ĐỊNH HƯỚNG CỐ ĐỊNH (0: Trái, 1: Giữa, 2: Phải)
		// Đảm bảo không bao giờ đi sai điểm đến dù server đổi tên map
		int reqDir = getRequiredDirection(currMap, nextMapId);
		return getWaypointByDirection(reqDir);
	}

	public static int getRequiredDirection(int curr, int next)
	{
		// TRÁI ĐẤT
		if (curr == 21 && next == 0) return 2; // Nhà Gohan -> Làng Aru: Phải
		if (curr == 0)
		{
			if (next == 21) return 0; // Làng Aru -> Nhà Gohan: Trái
			if (next == 1) return 2;  // Làng Aru -> Đồi hoa cúc: Phải
			if (next == 24) return 1; // Làng Aru -> Trạm tàu: Giữa/Trên
			if (next == 42) return 1; // Làng Aru -> Vách núi: Giữa/Dưới
		}
		if (curr == 1) { if (next == 0) return 0; if (next == 2) return 2; }
		if (curr == 2) { if (next == 1) return 0; if (next == 3) return 2; }
		if (curr == 3)
		{
			if (next == 2) return 0;  // Rừng nấm -> Thung lũng tre: Trái
			if (next == 4) return 2;  // Rừng nấm -> Rừng xương: Phải
			if (next == 27) return 1; // Rừng nấm -> Rừng bamboo: Giữa
		}
		if (curr == 4) { if (next == 3) return 0; if (next == 5) return 2; }
		if (curr == 5) { if (next == 4) return 0; if (next == 6) return 2; }
		if (curr == 6 && next == 5) return 0;
		if (curr == 27) { if (next == 3) return 0; if (next == 28) return 2; if (next == 53) return 1; }
		if (curr == 28) { if (next == 27) return 0; if (next == 29) return 2; }
		if (curr == 29) { if (next == 28) return 0; if (next == 30) return 2; }
		if (curr == 30 && next == 29) return 0;
		if (curr == 42 && next == 0) return 0;
		if (curr == 45 && next == 46) return 2;
		if (curr == 46) { if (next == 45) return 0; if (next == 48) return 2; }
		if (curr == 48) { if (next == 46) return 0; if (next == 49) return 2; }
		if (curr == 49) { if (next == 48) return 0; if (next == 50) return 2; }
		if (curr == 50 && next == 49) return 0;

		// NAMẾC
		if (curr == 22 && next == 7) return 2; // Nhà Moori -> Làng Mori: Phải
		if (curr == 7)
		{
			if (next == 22) return 0; // Làng Mori -> Nhà Moori: Trái
			if (next == 8) return 2;  // Làng Mori -> Đồi nấm tím: Phải
			if (next == 25) return 1; // Làng Mori -> Trạm tàu: Giữa
			if (next == 43) return 1; // Làng Mori -> Vách núi: Giữa
		}
		if (curr == 8) { if (next == 7) return 0; if (next == 9) return 2; }
		if (curr == 9)
		{
			if (next == 8) return 0;  // Thị trấn Moori -> Đồi nấm tím: Trái
			if (next == 11) return 2; // Thị trấn Moori -> Thung lũng Mây: Phải
			if (next == 31) return 1; // Thị trấn Moori -> Núi Ba Hòn: Giữa
		}
		if (curr == 11) { if (next == 9) return 0; if (next == 12) return 2; if (next == 31) return 1; }
		if (curr == 12) { if (next == 11) return 0; if (next == 13) return 2; }
		if (curr == 13) { if (next == 12) return 0; if (next == 10) return 2; }
		if (curr == 10 && next == 13) return 0;
		if (curr == 31) { if (next == 9 || next == 11) return 0; if (next == 32) return 2; }
		if (curr == 32) { if (next == 31) return 0; if (next == 33) return 2; }
		if (curr == 33) { if (next == 32) return 0; if (next == 34) return 2; }
		if (curr == 34 && next == 33) return 0;
		if (curr == 43 && next == 7) return 0;

		// XAYDA
		if (curr == 23 && next == 14) return 2; // Nhà Broly -> Làng Kakarot: Phải
		if (curr == 14)
		{
			if (next == 23) return 0; // Làng Kakarot -> Nhà Broly: Trái
			if (next == 15) return 2; // Làng Kakarot -> Đồi hoang: Phải
			if (next == 26) return 1; // Làng Kakarot -> Trạm tàu: Giữa
			if (next == 44) return 1; // Làng Kakarot -> Vách núi: Giữa
		}
		if (curr == 15) { if (next == 14) return 0; if (next == 16) return 2; }
		if (curr == 16) { if (next == 15) return 0; if (next == 17) return 2; }
		if (curr == 17)
		{
			if (next == 16) return 0; // Rừng nguyên sinh -> Làng Plant: Trái
			if (next == 18) return 2; // Rừng nguyên sinh -> Rừng cọ: Phải
			if (next == 35) return 1; // Rừng nguyên sinh -> Núi khỉ vàng: Giữa
		}
		if (curr == 18) { if (next == 17) return 0; if (next == 20) return 2; }
		if (curr == 20) { if (next == 18) return 0; if (next == 19) return 2; }
		if (curr == 19 && next == 20) return 0;
		if (curr == 35) { if (next == 17) return 0; if (next == 36) return 2; }
		if (curr == 36) { if (next == 35) return 0; if (next == 37) return 2; }
		if (curr == 37) { if (next == 36) return 0; if (next == 38) return 2; }
		if (curr == 38 && next == 37) return 0;
		if (curr == 44 && next == 14) return 0;

		// FIDE & NÚI TUYẾT (63 - 83)
		if (curr == 63) { if (next == 64) return 0; if (next == 68) return 2; }
		if (curr >= 64 && curr <= 67)
		{
			if (next == curr - 1) return 0;
			if (next == curr + 1) return 2;
		}
		if (curr == 68) { if (next == 63) return 0; if (next == 69) return 2; if (next == 79) return 1; }
		if (curr >= 69 && curr <= 78)
		{
			if (next == curr - 1) return 0;
			if (next == curr + 1) return 2;
		}
		if (curr >= 79 && curr <= 83)
		{
			if (next == 68 && curr == 79) return 0;
			if (next == curr - 1) return 0;
			if (next == curr + 1) return 2;
		}

		// TƯƠNG LAI (92 - 103)
		if (curr >= 92 && curr <= 103)
		{
			if (next < curr) return 0;
			if (next > curr) return 2;
		}

		// COLD (105 - 111)
		if (curr >= 105 && curr <= 111)
		{
			if (next < curr) return 0;
			if (next > curr) return 2;
		}

		// Mặc định: Nếu next > curr -> Phải (2), next < curr -> Trái (0)
		return (next > curr) ? 2 : 0;
	}

	public static Waypoint getWaypointByDirection(int dir)
	{
		if (TileMap.vGo == null || TileMap.vGo.size() == 0) return null;
		if (TileMap.vGo.size() == 1) return (Waypoint)TileMap.vGo.elementAt(0);

		int midMap = TileMap.pxw / 2;

		if (dir == 0) // Trái: Tìm Waypoint có minX nhỏ nhất
		{
			Waypoint bestWp = null;
			int minX = int.MaxValue;
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint wp = (Waypoint)TileMap.vGo.elementAt(i);
				if (wp != null && wp.minX < minX)
				{
					minX = wp.minX;
					bestWp = wp;
				}
			}
			return bestWp;
		}
		else if (dir == 2) // Phải: Tìm Waypoint có maxX lớn nhất
		{
			Waypoint bestWp = null;
			int maxX = int.MinValue;
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint wp = (Waypoint)TileMap.vGo.elementAt(i);
				if (wp != null && wp.maxX > maxX)
				{
					maxX = wp.maxX;
					bestWp = wp;
				}
			}
			return bestWp;
		}
		else // Giữa / Cổng phụ: Ưu tiên isEnter, isOffline, hoặc gần tâm map
		{
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint wp = (Waypoint)TileMap.vGo.elementAt(i);
				if (wp != null && (wp.isEnter || wp.isOffline))
				{
					return wp;
				}
			}
			Waypoint midWp = null;
			int bestDist = int.MaxValue;
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint wp = (Waypoint)TileMap.vGo.elementAt(i);
				if (wp != null)
				{
					int wpMid = (wp.minX + wp.maxX) / 2;
					int dist = System.Math.Abs(wpMid - midMap);
					if (dist < bestDist)
					{
						bestDist = dist;
						midWp = wp;
					}
				}
			}
			return midWp;
		}
	}

	// Cập nhật Xmap mỗi khung hình
	public static void update(long now)
	{
		updateGoback(now);

		if (TileMap.mapID != lastMapIdChecked)
		{
			lastMapIdChecked = TileMap.mapID;
			learnCurrentMapWaypoints();
		}

		if (!isActive || currentPath == null || currentPath.Count == 0)
		{
			return;
		}

		Char myChar = Char.myCharz();
		if (myChar == null || TileMap.mapID == -1 || Controller.isStopReadMessage)
		{
			return;
		}

		// Kiểm tra nếu đã đến map đích
		if (TileMap.mapID == targetMapId)
		{
			isActive = false;
			currentPath.Clear();
			string destName = getMapName(targetMapId);
			ModManager.notify("🎉 Đã tới đích: " + destName);

			if (isGobacking)
			{
				isGobacking = false;
				// Chuyển về đúng khu cũ nếu khác khu
				if (gobackZoneId >= 0 && TileMap.zoneID != gobackZoneId)
				{
					Service.gI().requestChangeZone(gobackZoneId, -1);
				}
				// Dịch lại đúng tọa độ cũ
				if (gobackX > 0 && gobackY > 0)
				{
					myChar.cx = gobackX;
					myChar.cy = gobackY;
					myChar.cxSend = gobackX;
					myChar.cySend = gobackY;
					ModManager.sendDirectMovePacket(gobackX, gobackY);
				}
				// Tự bật lại Tàn sát / Auto train
				if (wasTanSatActive)
				{
					ModManager.isTanSat = true;
				}
				if (wasAutoTrainActive)
				{
					ModManager.isAutoTrain = true;
				}
				ModManager.notify("✅ Goback hoàn tất! Tiếp tục up quái khu " + gobackZoneId);
			}
			return;
		}

		// 1. Xử lý phản hồi Capsule nếu đang đợi bảng dịch chuyển Capsule
		if (isWaitingCapsule)
		{
			if (GameCanvas.panel != null && GameCanvas.panel.isShow && GameCanvas.panel.type == 14 && GameCanvas.panel.mapNames != null)
			{
				string targetNorm = removeAccents(getMapName(targetMapId)).ToLower();
				string targetPlanet = getPlanetNameOfMap(targetMapId);
				int exactMatchIdx = -1;
				int planetMatchIdx = -1;

				for (int i = 0; i < GameCanvas.panel.mapNames.Length; i++)
				{
					if (string.IsNullOrEmpty(GameCanvas.panel.mapNames[i])) continue;
					string mName = removeAccents(GameCanvas.panel.mapNames[i]).ToLower();
					if (mName.Equals(targetNorm) || (targetNorm.Length >= 4 && mName.Contains(targetNorm)))
					{
						exactMatchIdx = i;
						break;
					}
					if (planetMatchIdx == -1 && !string.IsNullOrEmpty(targetPlanet) && GameCanvas.panel.planetNames != null && i < GameCanvas.panel.planetNames.Length)
					{
						string pName = removeAccents(GameCanvas.panel.planetNames[i]).ToLower();
						if (pName.Contains(targetPlanet))
						{
							planetMatchIdx = i;
						}
					}
				}

				int chosen = (exactMatchIdx != -1) ? exactMatchIdx : planetMatchIdx;
				if (chosen != -1)
				{
					string chosenName = GameCanvas.panel.mapNames[chosen];
					ModManager.notify("🚀 Capsule bay tới: " + chosenName);
					Service.gI().requestMapSelect(chosen);
					GameCanvas.panel.hide();
					isWaitingCapsule = false;
					lastStepTime = now + 1500L;
					return;
				}
				else
				{
					// Không có map đích trong Capsule -> Đóng và tiếp tục đi đường bộ / tàu vũ trụ
					GameCanvas.panel.hide();
					isWaitingCapsule = false;
				}
			}
			else if (now - capsuleWaitStartTime > 1800L)
			{
				// Quá 1.8s không mở được bảng Capsule -> Bỏ qua Capsule
				isWaitingCapsule = false;
			}
			else
			{
				return;
			}
		}

		// Đang trong tiến trình chuyển map, chờ server load map
		if (Char.ischangingMap)
		{
			if (now - ModManager.lastChangeMapRequestTime > 4000L)
			{
				Char.ischangingMap = false;
			}
			lastStepTime = now;
			return;
		}

		// Đợi tối thiểu 600ms sau khi map mới nạp xong để dữ liệu client/server ổn định 100%
		if (now - ModManager.lastMapChangeCompletedTime < 600L)
		{
			return;
		}

		// Đảm bảo không bao giờ spam bước: Mỗi thao tác waypoint cách nhau ít nhất 1000ms
		if (now - lastStepTime < 1000L)
		{
			return;
		}

		// Đảm bảo map đã tải xong các cổng waypoint
		if (TileMap.vGo == null || TileMap.vGo.size() == 0)
		{
			return;
		}

		// Tìm vị trí map hiện tại trong lộ trình
		int currIndex = currentPath.IndexOf(TileMap.mapID);
		if (currIndex == -1)
		{
			// Bị lệch đường (do vừa bay Capsule hoặc rơi vào map khác), tính toán lại lộ trình từ map hiện tại
			List<int> newPath = findPath(TileMap.mapID, targetMapId);
			if (newPath != null && newPath.Count >= 2)
			{
				currentPath = newPath;
				currentPathIndex = 0;
				lastStepTime = now;
				return;
			}
			else
			{
				stopXmap();
				ModManager.notify("Mất dấu đường đi, đã dừng Xmap!");
				return;
			}
		}

		currentPathIndex = currIndex;
		if (currentPathIndex >= currentPath.Count - 1)
		{
			return;
		}

		int nextMapId = currentPath[currentPathIndex + 1];

		// Xử lý chuyển hành tinh qua NPC Tàu vũ trụ (ở Trạm tàu 24, 25, 26)
		if ((TileMap.mapID == 24 || TileMap.mapID == 25 || TileMap.mapID == 26) &&
			(nextMapId == 24 || nextMapId == 25 || nextMapId == 26 || nextMapId == 84))
		{
			Npc transportNpc = null;
			if (GameScr.vNpc != null)
			{
				for (int i = 0; i < GameScr.vNpc.size(); i++)
				{
					Npc n = (Npc)GameScr.vNpc.elementAt(i);
					if (n != null && n.template != null)
					{
						string nName = n.template.name.ToLower();
						if (nName.Contains("brief") || nName.Contains("cargo") || nName.Contains("cui") ||
							n.template.npcTemplateId == 13 || n.template.npcTemplateId == 14 || n.template.npcTemplateId == 15)
						{
							transportNpc = n;
							break;
						}
					}
				}
			}

			if (transportNpc != null)
			{
				myChar.cx = transportNpc.cx;
				myChar.cy = transportNpc.cy;
				ModManager.sendDirectMovePacket(transportNpc.cx, transportNpc.cy);

				if (GameCanvas.menu != null && GameCanvas.menu.showMenu && GameCanvas.menu.menuItems != null && GameCanvas.menu.menuItems.size() > 0)
				{
					string targetKeyword = (nextMapId == 24) ? "trai dat" : ((nextMapId == 25) ? "namec" : ((nextMapId == 26) ? "xayda" : "sieu thi"));
					Command targetCmd = null;
					int targetIdx = -1;
					for (int i = 0; i < GameCanvas.menu.menuItems.size(); i++)
					{
						Command cmd = (Command)GameCanvas.menu.menuItems.elementAt(i);
						if (cmd != null && !string.IsNullOrEmpty(cmd.caption))
						{
							string cap = removeAccents(cmd.caption).ToLower();
							if (cap.Contains(targetKeyword))
							{
								targetCmd = cmd;
								targetIdx = i;
								break;
							}
						}
					}
					if (targetCmd != null)
					{
						GameCanvas.menu.menuSelectedItem = targetIdx;
						targetCmd.performAction();
						GameCanvas.menu.doCloseMenu();
						lastStepTime = now + 1500L;
						ModManager.notify("🚀 Lên tàu sang " + getMapName(nextMapId));
						return;
					}
				}
				else
				{
					Service.gI().openMenu(transportNpc.template.npcTemplateId);
					lastStepTime = now + 600L;
					return;
				}
			}
			return;
		}

		// Tìm waypoint dẫn sang nextMapId chuẩn xác 100% bằng tên và bảng định hướng bản đồ
		Waypoint wp = findWaypointLeadingTo(TileMap.mapID, nextMapId);
		if (wp != null)
		{
			retryCount = 0;
			lastStepTime = now;
			ModManager.teleportToWaypoint(wp, -1);
		}
		else
		{
			retryCount++;
			lastStepTime = now + 600L;
			if (retryCount > 4)
			{
				stopXmap();
				ModManager.notify("Không tìm thấy cổng sang " + getMapName(nextMapId));
			}
		}
	}

	// ==========================================
	// HỆ THỐNG GOBACK TỰ ĐỘNG KHI CHẾT & HẾT KI
	// ==========================================
	public static bool isHomeMap(int mapId)
	{
		return mapId == 21 || mapId == 22 || mapId == 23;
	}

	public static int getHomeMapId()
	{
		Char myChar = Char.myCharz();
		if (myChar == null) return 21;
		if (myChar.cgender == 1) return 22; // Namếc (Nhà Moori)
		if (myChar.cgender == 2) return 23; // Xayda (Nhà Broly)
		return 21; // Trái Đất (Nhà Gohan)
	}

	public static void toggleGoback()
	{
		isGoback = !isGoback;
		if (isGoback)
		{
			isManualTravel = false;
			Char myChar = Char.myCharz();
			if (myChar != null && !isHomeMap(TileMap.mapID))
			{
				if (!hasSavedGobackPos || gobackMapId == -1)
				{
					gobackMapId = TileMap.mapID;
					gobackZoneId = TileMap.zoneID;
					gobackX = myChar.cx;
					gobackY = myChar.cy;
					ModManager.gobackPosText = gobackMapId + ", " + gobackZoneId + ", " + gobackX + ", " + gobackY;
				}
			}
			if (gobackMapId != -1)
			{
				Pk9rXmap.IdMapCapsuleReturn = gobackMapId;
			}
		}
		else if (!hasSavedGobackPos)
		{
			stopAllGobackAndXmap();
		}
		ModManager.notify("Tự động Goback: " + (isGoback ? "BẬT" : "TẮT"));
	}

	public static void toggleGobackKi()
	{
		isGobackKi = !isGobackKi;
		if (isGobackKi)
		{
			Char myChar = Char.myCharz();
			if (myChar != null && !isHomeMap(TileMap.mapID))
			{
				if (!hasSavedGobackPos || gobackMapId == -1)
				{
					gobackMapId = TileMap.mapID;
					gobackZoneId = TileMap.zoneID;
					gobackX = myChar.cx;
					gobackY = myChar.cy;
					hasSavedGobackPos = true;
				}
			}
			ModManager.notify("Về nhà khi hết KI: BẬT");

			if (!isGoback)
			{
				ModManager.notify("⚠️ Cần bật cả 'Tự động Goback' để đủ điều kiện kích hoạt!");
			}
		}
		else
		{
			isReturningHomeForKi = false;
			ModManager.notify("Về nhà khi hết KI: TẮT");
		}
	}

	public static void saveCurrentPosAsGoback()
	{
		Char myChar = Char.myCharz();
		if (myChar != null && !isHomeMap(TileMap.mapID))
		{
			gobackMapId = TileMap.mapID;
			gobackZoneId = TileMap.zoneID;
			gobackX = myChar.cx;
			gobackY = myChar.cy;
			hasSavedGobackPos = true;
			isManualTravel = false;
			Pk9rXmap.IdMapCapsuleReturn = gobackMapId;
			ModManager.gobackPosText = gobackMapId + ", " + gobackZoneId + ", " + gobackX + ", " + gobackY;
			ModManager.notify("📌 Đã lưu vị trí: " + getMapName(gobackMapId) + " [Khu " + gobackZoneId + "] (" + gobackX + ", " + gobackY + ")");
		}
		else
		{
			ModManager.notify("⚠️ Không thể lưu toạ độ khi đang ở trong Nhà!");
		}
	}

	public static void clearSavedGobackPos()
	{
		hasSavedGobackPos = false;
		isManualTravel = false;
		gobackMapId = -1;
		gobackZoneId = -1;
		gobackX = -1;
		gobackY = -1;
		ModManager.gobackPosText = "";
		stopAllGobackAndXmap();
		ModManager.notify("🗑️ Đã xoá toạ độ & huỷ toàn bộ Goback/Xmap!");
	}

	public static void stopAllGobackAndXmap()
	{
		isGobacking = false;
		isReturningHomeForKi = false;
		isActive = false;
		targetMapId = -1;
		currentPath = null;
		XmapController.FinishXmap();
		if (GameCanvas.panel != null && GameCanvas.panel.isShow && GameCanvas.panel.type == 14)
		{
			GameCanvas.panel.hide();
		}
	}

	public static bool parseGobackPosText(string text, out int mapId, out int zoneId, out int x, out int y)
	{
		mapId = -1; zoneId = -1; x = -1; y = -1;
		if (string.IsNullOrEmpty(text)) return false;
		char[] seps = new char[] { ',', '-', ' ', ';' };
		string[] parts = text.Split(seps, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length >= 4)
		{
			if (int.TryParse(parts[0], out mapId) &&
				int.TryParse(parts[1], out zoneId) &&
				int.TryParse(parts[2], out x) &&
				int.TryParse(parts[3], out y))
			{
				return true;
			}
		}
		return false;
	}

	private static void updateGoback(long now)
	{
		Char myChar = Char.myCharz();
		if (myChar == null) return;

		// 0. Bù trễ đồng bộ vị trí sau khi load map hoặc chuyển khu
		if (pendingSyncGobackPosTime > 0L && now >= pendingSyncGobackPosTime)
		{
			pendingSyncGobackPosTime = 0L;
			if (gobackX > 0 && gobackY > 0 && TileMap.mapID == gobackMapId)
			{
				int targetY = gobackY;
				int groundY = ModManager.findGroundY(gobackX);
				if (groundY > 0 && Res.abs(groundY - gobackY) <= 80)
				{
					targetY = groundY;
				}

				myChar.cx = gobackX;
				myChar.cy = targetY;
				myChar.cxSend = gobackX;
				myChar.cySend = targetY;
				myChar.currentMovePoint = null;
				myChar.cvx = (myChar.cvy = 0);
				bool onGround = TileMap.tileTypeAt(myChar.cx, myChar.cy, 2) || ((TileMap.tileTypeAtPixel(myChar.cx, myChar.cy) & 2) == 2);
				myChar.statusMe = onGround ? 1 : 4;
				ModManager.sendDirectMovePacket(gobackX, targetY);
				Service.gI().charMove();
			}
		}

		// Đang chuyển map hoặc map đang load thì không thực hiện lệnh để tránh spam xung đột
		if (Char.isLoadingMap || Char.ischangingMap || Controller.isStopReadMessage)
		{
			return;
		}

		if (XmapController.IsXmapRunning && XmapController.IdMapEnd != gobackMapId && (!isReturningHomeForKi || XmapController.IdMapEnd != getHomeMapId()))
		{
			// Đang chủ động chạy Xmap tới map khác: tạm dừng goback
			isGobacking = false;
			return;
		}

		// =========================================================================
		// 1. TỰ ĐỘNG HỒI SINH KHI CHẾT (Dù là Goback hay Khoá toạ độ / Khoá map)
		// =========================================================================
		if ((myChar.statusMe == 14 || myChar.cHP <= 0) && (isGoback || hasSavedGobackPos))
		{
			isManualTravel = false; // Khi nhân vật chết, kích hoạt lại Goback tự động
			isGobacking = true;
			isReturningHomeForKi = false;
			if (ModManager.isTanSat) wasTanSatActive = true;
			if (ModManager.isAutoTrain) wasAutoTrainActive = true;
			ModManager.isTanSat = false;
			ModManager.isAutoTrain = false;

			if (gobackMapId == -1 && !isHomeMap(TileMap.mapID))
			{
				gobackMapId = TileMap.mapID;
				gobackZoneId = TileMap.zoneID;
				gobackX = myChar.cx;
				gobackY = myChar.cy;
				ModManager.gobackPosText = gobackMapId + ", " + gobackZoneId + ", " + gobackX + ", " + gobackY;
			}

			// Đảm bảo Capsule 'Về chỗ cũ' tự động đưa về đúng map đã lưu
			if (gobackMapId != -1)
			{
				Pk9rXmap.IdMapCapsuleReturn = gobackMapId;
			}

			// Huỷ tiến trình Xmap cũ đang chạy dở để tránh bị kẹt trạng thái
			XmapController.FinishXmap();

			if (now - lastRespawnWaitTime > 500L)
			{
				lastRespawnWaitTime = now;
				ModManager.notify("⚠️ Bị chết! Đang tự động về nhà hồi sinh...");
				Service.gI().returnTownFromDead();
			}
			return;
		}

		// =========================================================================
		// 2. TỰ ĐỘNG VỀ NHÀ KHI HẾT KI (Ô "Về nhà khi hết KI")
		// =========================================================================
		if (isGobackKi && !isGobacking && !isReturningHomeForKi)
		{
			if (!isHomeMap(TileMap.mapID) && myChar.cHP > 0 && myChar.statusMe != 14)
			{
				if (now - lastKiCheckTime >= 1000L)
				{
					lastKiCheckTime = now;
					// Kiểm tra KI cạn kiệt (dưới 5% hoặc <= 20)
					if (myChar.cMP <= 20 || (myChar.cMPFull > 0 && myChar.cMP < (myChar.cMPFull * 5 / 100)))
					{
						myChar.doUsePotion();

						if (myChar.cMP <= 20 || (myChar.cMPFull > 0 && myChar.cMP < (myChar.cMPFull * 5 / 100)))
						{
							if (gobackMapId == -1)
							{
								gobackMapId = TileMap.mapID;
								gobackZoneId = TileMap.zoneID;
								gobackX = myChar.cx;
								gobackY = myChar.cy;
								ModManager.gobackPosText = gobackMapId + ", " + gobackZoneId + ", " + gobackX + ", " + gobackY;
							}

							isReturningHomeForKi = true;
							isGobacking = true;
							if (ModManager.isTanSat) wasTanSatActive = true;
							if (ModManager.isAutoTrain) wasAutoTrainActive = true;
							ModManager.isTanSat = false;
							ModManager.isAutoTrain = false;

							int homeMapId = getHomeMapId();
							ModManager.notify("⚠️ Hết KI! Đang tự động bay về nhà ăn đùi gà hồi phục...");
							startXmap(homeMapId);
							return;
						}
					}
				}
			}
		}

		// Đang ở nhà để hồi phục KI:
		if (isReturningHomeForKi && isHomeMap(TileMap.mapID))
		{
			if (myChar.statusMe != 14 && myChar.cHP > 0)
			{
				if (myChar.cHP < myChar.cHPFull * 90 / 100 || myChar.cMP < myChar.cMPFull * 90 / 100)
				{
					myChar.doUsePotion();
				}

				if (myChar.cMP >= myChar.cMPFull * 80 / 100)
				{
					isReturningHomeForKi = false;
					ModManager.notify("🏠 Đã hồi đủ KI! Chuẩn bị quay lại map...");
				}
				else
				{
					return;
				}
			}
		}

		// =========================================================================
		// 3. TỰ ĐỘNG GOBACK & KHOÁ MAP / KHOÁ TOẠ ĐỘ (TỰ ĐỘNG ÁP DỤNG XMAP)
		// =========================================================================
		if ((isGoback || hasSavedGobackPos) && gobackMapId != -1 && myChar.cHP > 0 && myChar.statusMe != 14)
		{
			// Đảm bảo Capsule 'Về chỗ cũ' luôn có đích đến chính xác
			Pk9rXmap.IdMapCapsuleReturn = gobackMapId;

			// TRƯỜNG HỢP 1: ĐANG Ở KHÁC MAP (Ở nhà sau hồi sinh, hoặc bị văng/đi lạc map):
			if (TileMap.mapID != gobackMapId)
			{
				// Tranh thủ ăn đậu nếu có nhưng TUYỆT ĐỐI KHÔNG CHỜ/CHẶN HP
				if (isHomeMap(TileMap.mapID) && (myChar.cHP < myChar.cHPFull * 90 / 100 || myChar.cMP < myChar.cMPFull * 90 / 100))
				{
					myChar.doUsePotion();
				}

				// Nếu Xmap đang chạy và đích đến đúng là gobackMapId:
				if (XmapController.IsXmapRunning && XmapController.IdMapEnd == gobackMapId)
				{
					// Theo dõi chuyển map để chống kẹt
					if (TileMap.mapID != lastMapIdTracked)
					{
						lastMapIdTracked = TileMap.mapID;
						lastMapChangeTime = now;
					}
					else if (now - lastMapChangeTime > 5000L)
					{
						// Nếu kẹt quá 5s tại 1 map: Chạy lại Xmap
						lastMapChangeTime = now;
						startXmap(gobackMapId);
					}
					return;
				}

				// TỰ ĐỘNG KÍCH HOẠT XMAP NGAY LẬP TỨC (nhanh và mượt như bấm Z -> chọn map)
				if (now - lastXmapRequestTime > 300L)
				{
					lastXmapRequestTime = now;
					lastMapIdTracked = TileMap.mapID;
					lastMapChangeTime = now;
					isGobacking = true;
					ModManager.notify("🚀 Tự động bay về " + getMapName(gobackMapId) + " [Khu " + gobackZoneId + "]...");
					startXmap(gobackMapId);
					return;
				}
			}
			// TRƯỜNG HỢP 2: ĐÃ Ở ĐÚNG MAP MỤC TIÊU:
			else
			{
				isGobacking = false;

				// Nếu Xmap vẫn còn cờ đang chạy tới map này thì dừng lại
				if (XmapController.IsXmapRunning && XmapController.IdMapEnd == gobackMapId)
				{
					XmapController.FinishXmap();
				}

				// Kiểm tra khu: nếu khác khu mục tiêu thì đổi khu
				if (gobackZoneId >= 0 && TileMap.zoneID != gobackZoneId && now - lastZoneRequestTime > 2500L)
				{
					lastZoneRequestTime = now;
					Service.gI().requestChangeZone(gobackZoneId, -1);
					pendingSyncGobackPosTime = now + 500L;
				}

				// Nếu có BẬT KHOÁ TOẠ ĐỘ / KHOÁ MAP: nếu lệch toạ độ -> LÔI VỀ NGAY
				if (hasSavedGobackPos && gobackX > 0 && gobackY > 0)
				{
					int dist = Res.distance(myChar.cx, myChar.cy, gobackX, gobackY);
					if (dist > 30 && now - lastPullPosTime > 600L)
					{
						lastPullPosTime = now;
						int targetY = gobackY;
						int groundY = ModManager.findGroundY(gobackX);
						if (groundY > 0 && Res.abs(groundY - gobackY) <= 80)
						{
							targetY = groundY;
						}

						myChar.cx = gobackX;
						myChar.cy = targetY;
						myChar.cxSend = gobackX;
						myChar.cySend = targetY;
						myChar.currentMovePoint = null;
						myChar.cvx = (myChar.cvy = 0);
						bool onGround = TileMap.tileTypeAt(myChar.cx, myChar.cy, 2) || ((TileMap.tileTypeAtPixel(myChar.cx, myChar.cy) & 2) == 2);
						myChar.statusMe = onGround ? 1 : 4;
						ModManager.sendDirectMovePacket(gobackX, targetY);
						Service.gI().charMove();
					}
					else if (now - lastPullPosTime > 1200L)
					{
						// Tránh lơ lửng nếu đứng giữa không trung
						bool onGround = TileMap.tileTypeAt(myChar.cx, myChar.cy, 2) || ((TileMap.tileTypeAtPixel(myChar.cx, myChar.cy) & 2) == 2);
						if (!onGround && myChar.statusMe != 4 && myChar.statusMe != 2)
						{
							myChar.statusMe = 4;
						}
					}
				}
			}
		}
	}

	// ==========================================
	// BẢNG GIAO DIỆN CHỌN MAP DỊCH CHUYỂN (GUI DIALOG)
	// ==========================================
	public static bool isShowXmapDialog = false;
	public static int activeXmapPlanetTab = 0;
	public static int scrollXmapIndex = 0;

	public static int[] getPlanetMapList(int planet)
	{
		switch (planet)
		{
			case 0: // Trái Đất (22 map)
				return new int[] { 21, 0, 1, 2, 3, 4, 5, 6, 24, 27, 28, 29, 30, 42, 45, 46, 47, 48, 49, 50, 51, 52 };
			case 1: // Namếc (14 map)
				return new int[] { 22, 7, 8, 9, 11, 12, 13, 10, 25, 31, 32, 33, 34, 43 };
			case 2: // Xayda (14 map)
				return new int[] { 23, 14, 15, 16, 17, 18, 20, 19, 26, 35, 36, 37, 38, 44 };
			case 3: // Fide & Núi Tuyết (21 map)
				return new int[] { 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83 };
			case 4: // Hành Tinh Cold (7 map)
				return new int[] { 105, 106, 107, 108, 109, 110, 111 };
			case 5: // Tương Lai (10 map)
				return new int[] { 92, 93, 94, 96, 97, 98, 99, 100, 102, 103 };
			case 6: // Phụ Bản & Bản Đồ Đặc Biệt (26 map)
				return new int[] { 84, 112, 113, 129, 130, 131, 132, 133, 135, 136, 137, 138, 141, 142, 143, 144, 147, 148, 149, 151, 152, 114, 115, 117, 120, 53 };
			default:
				return new int[0];
		}
	}

	public static void showXmapMenu()
	{
		ModManager.closeAllDialogs();
		XmapController.ShowXmapMenu();
	}

	public static void onGobackArrived()
	{
		if (!isGoback && !hasSavedGobackPos) return;

		// Nếu vừa đến Nhà để hồi KI: KHÔNG kết thúc Goback, để updateGoback tiếp tục theo dõi hồi phục rồi mới đi tiếp về gobackMapId!
		if (isReturningHomeForKi && isHomeMap(TileMap.mapID))
		{
			lastRespawnWaitTime = mSystem.currentTimeMillis();
			ModManager.notify("🏠 Đã về nhà! Đang chờ hồi phục đầy đủ KI...");
			return;
		}

		if (TileMap.mapID == gobackMapId)
		{
			isGobacking = false;
			isReturningHomeForKi = false;
			isManualTravel = false;

			Char myChar = Char.myCharz();
			if (gobackZoneId >= 0 && TileMap.zoneID != gobackZoneId)
			{
				Service.gI().requestChangeZone(gobackZoneId, -1);
				pendingSyncGobackPosTime = mSystem.currentTimeMillis() + 600L;
			}

			if (myChar != null && gobackX > 0 && gobackY > 0)
			{
				int targetY = gobackY;
				int groundY = ModManager.findGroundY(gobackX);
				if (groundY > 0 && Res.abs(groundY - gobackY) <= 80)
				{
					targetY = groundY;
				}

				myChar.cx = gobackX;
				myChar.cy = targetY;
				myChar.cxSend = gobackX;
				myChar.cySend = targetY;
				myChar.currentMovePoint = null;
				myChar.cvx = (myChar.cvy = 0);
				bool onGround = TileMap.tileTypeAt(myChar.cx, myChar.cy, 2) || ((TileMap.tileTypeAtPixel(myChar.cx, myChar.cy) & 2) == 2);
				myChar.statusMe = onGround ? 1 : 4;
				ModManager.sendDirectMovePacket(gobackX, targetY);
				Service.gI().charMove();
			}

			if (wasTanSatActive) ModManager.isTanSat = true;
			if (wasAutoTrainActive) ModManager.isAutoTrain = true;
			ModManager.notify("✅ Đã đến vị trí mục tiêu: " + getMapName(gobackMapId) + " [Khu " + gobackZoneId + "] (" + gobackX + ", " + gobackY + ")");
		}
	}

	public static void paintXmapDialog(mGraphics g)
	{
		int boxW = 310;
		int boxH = 226;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;
		int headerH = 24;

		// 1. Nền vàng cát & viền đậm
		g.setColor(14860437);
		g.fillRect(boxX, boxY, boxW, boxH);
		g.setColor(2758924);
		g.drawRect(boxX, boxY, boxW, boxH);

		// 2. Tiêu đề xanh: BẢN ĐỒ DỊCH CHUYỂN (XMAP)
		g.setColor(1800936);
		g.fillRect(boxX, boxY, boxW, headerH);
		mFont.tahoma_7b_white.drawString(g, "BẢN ĐỒ DỊCH CHUYỂN (XMAP)", boxX + boxW / 2, boxY + 5, mFont.CENTER);

		// Nút [X] đóng ở góc trên bên phải
		int btnCloseX = boxX + boxW - 20;
		int btnCloseY = boxY + 4;
		g.setColor(13382451);
		g.fillRect(btnCloseX, btnCloseY, 16, 16);
		g.setColor(16777215);
		g.drawRect(btnCloseX, btnCloseY, 16, 16);
		mFont.tahoma_7b_white.drawString(g, "X", btnCloseX + 8, btnCloseY + 2, mFont.CENTER);

		// 3. 7 Tab Hành Tinh
		string[] tabNames = new string[] { "T.Đất", "Namếc", "Xayda", "Fide", "Cold", "T.Lai", "Đ.Biệt" };
		int tabH = 20;
		int tabW = (boxW - 12) / 7;
		int tabY = boxY + headerH + 3;

		for (int t = 0; t < 7; t++)
		{
			int curTabX = boxX + 6 + t * tabW;
			bool isSelected = (activeXmapPlanetTab == t);
			if (isSelected)
			{
				g.setColor(1800936);
				g.fillRect(curTabX, tabY, tabW, tabH);
				g.setColor(16777215);
				g.drawRect(curTabX, tabY, tabW, tabH);
				mFont.tahoma_7b_white.drawString(g, tabNames[t], curTabX + tabW / 2, tabY + 4, mFont.CENTER);
			}
			else
			{
				g.setColor(12693910);
				g.fillRect(curTabX, tabY, tabW, tabH);
				g.setColor(5985856);
				g.drawRect(curTabX, tabY, tabW, tabH);
				mFont.tahoma_7b_dark.drawString(g, tabNames[t], curTabX + tabW / 2, tabY + 4, mFont.CENTER);
			}
		}

		// 4. Khung danh sách Map
		int listY = tabY + tabH + 4;
		int listH = 145;
		g.setColor(15720130);
		g.fillRect(boxX + 6, listY, boxW - 12, listH);
		g.setColor(2758924);
		g.drawRect(boxX + 6, listY, boxW - 12, listH);

		int[] mapList = getPlanetMapList(activeXmapPlanetTab);
		int maxVisible = 7;
		int rowH = 20;

		for (int i = 0; i < maxVisible && (scrollXmapIndex + i) < mapList.Length; i++)
		{
			int mIdx = scrollXmapIndex + i;
			int mId = mapList[mIdx];
			string mName = getMapName(mId);
			int rowY = listY + 3 + i * rowH;

			// Sọc xen kẽ
			g.setColor((i % 2 == 0) ? 15263976 : 14474460);
			g.fillRect(boxX + 8, rowY, boxW - 16, rowH - 2);

			if (mId == TileMap.mapID)
			{
				mFont.tahoma_7b_green.drawString(g, "[" + mId + "] " + mName + "  (📍 Hiện tại)", boxX + 14, rowY + 3, 0);
			}
			else if (isActive && mId == targetMapId)
			{
				mFont.tahoma_7b_red.drawString(g, "🚀 [" + mId + "] " + mName + "  (Đang đi...)", boxX + 14, rowY + 3, 0);
			}
			else
			{
				mFont.tahoma_7b_dark.drawString(g, "[" + mId + "] " + mName, boxX + 14, rowY + 3, 0);
			}
		}

		// Mũi tên cuộn lên / xuống
		if (scrollXmapIndex > 0)
		{
			ModManager.drawTriangleUp(g, boxX + boxW - 12, listY + 6);
		}
		if (scrollXmapIndex + maxVisible < mapList.Length)
		{
			ModManager.drawTriangleDown(g, boxX + boxW - 12, listY + listH - 10);
		}

		// 5. Thanh điều khiển dưới cùng (Bottom bar)
		int bottomY = listY + listH + 4;
		int btnH = 22;

		if (isActive)
		{
			// Nút Dừng Xmap (Màu đỏ)
			int btnStopW = 120;
			int btnStopX = boxX + 8;
			g.setColor(13382451);
			g.fillRect(btnStopX, bottomY, btnStopW, btnH);
			g.setColor(2758924);
			g.drawRect(btnStopX, bottomY, btnStopW, btnH);
			mFont.tahoma_7b_white.drawString(g, "❌ DỪNG XMAP", btnStopX + btnStopW / 2, bottomY + 5, mFont.CENTER);

			// Nút Goback
			int btnGbW = boxW - btnStopW - 24;
			int btnGbX = btnStopX + btnStopW + 8;
			g.setColor(isGoback ? 2627855 : 12693910);
			g.fillRect(btnGbX, bottomY, btnGbW, btnH);
			g.setColor(2758924);
			g.drawRect(btnGbX, bottomY, btnGbW, btnH);
			if (isGoback)
				mFont.tahoma_7b_white.drawString(g, "🔄 Goback: BẬT", btnGbX + btnGbW / 2, bottomY + 5, mFont.CENTER);
			else
				mFont.tahoma_7b_dark.drawString(g, "🔄 Goback: TẮT", btnGbX + btnGbW / 2, bottomY + 5, mFont.CENTER);
		}
		else
		{
			// Nút Goback bên phải
			int btnGbW = 130;
			int btnGbX = boxX + boxW - btnGbW - 8;
			g.setColor(isGoback ? 2627855 : 12693910);
			g.fillRect(btnGbX, bottomY, btnGbW, btnH);
			g.setColor(2758924);
			g.drawRect(btnGbX, bottomY, btnGbW, btnH);
			if (isGoback)
				mFont.tahoma_7b_white.drawString(g, "🔄 Goback: BẬT", btnGbX + btnGbW / 2, bottomY + 5, mFont.CENTER);
			else
				mFont.tahoma_7b_dark.drawString(g, "🔄 Goback: TẮT", btnGbX + btnGbW / 2, bottomY + 5, mFont.CENTER);

			// Hướng dẫn bên trái
			mFont.tahoma_7_grey.drawString(g, "💡 Click map để tự bay đến!", boxX + 12, bottomY + 5, 0);
		}
	}

	public static void handleXmapDialogInput()
	{
		int boxW = 310;
		int boxH = 226;
		int boxX = GameCanvas.w / 2 - boxW / 2;
		int boxY = GameCanvas.h / 2 - boxH / 2;
		int headerH = 24;

		int[] mapList = getPlanetMapList(activeXmapPlanetTab);
		int maxVisible = 7;

		// Hỗ trợ cuộn chuột
		try
		{
			float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
			if (scrollWheel < 0f && scrollXmapIndex + maxVisible < mapList.Length)
			{
				scrollXmapIndex++;
			}
			else if (scrollWheel > 0f && scrollXmapIndex > 0)
			{
				scrollXmapIndex--;
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

			// 1. Nút [X] đóng bảng
			if (px >= boxX + boxW - 24 && px <= boxX + boxW && py >= boxY && py <= boxY + headerH)
			{
				isShowXmapDialog = false;
				GameCanvas.isPointerJustRelease = false;
				return;
			}

			// 2. Chuyển Tab Hành Tinh
			int tabH = 20;
			int tabW = (boxW - 12) / 7;
			int tabY = boxY + headerH + 3;
			if (py >= tabY && py <= tabY + tabH)
			{
				for (int t = 0; t < 7; t++)
				{
					int curTabX = boxX + 6 + t * tabW;
					if (px >= curTabX && px <= curTabX + tabW)
					{
						activeXmapPlanetTab = t;
						scrollXmapIndex = 0;
						GameCanvas.isPointerJustRelease = false;
						return;
					}
				}
			}

			// 3. Click chọn Map trong danh sách
			int listY = tabY + tabH + 4;
			int listH = 145;
			int rowH = 20;

			if (px >= boxX + 6 && px <= boxX + boxW - 6 && py >= listY && py <= listY + listH)
			{
				for (int i = 0; i < maxVisible && (scrollXmapIndex + i) < mapList.Length; i++)
				{
					int rowY = listY + 3 + i * rowH;
					if (py >= rowY && py <= rowY + rowH)
					{
						int mIdx = scrollXmapIndex + i;
						int selectedMapId = mapList[mIdx];
						isShowXmapDialog = false;
						startXmap(selectedMapId);
						GameCanvas.isPointerJustRelease = false;
						return;
					}
				}
			}

			// 4. Các nút dưới cùng (Bottom bar)
			int bottomY = listY + listH + 4;
			int btnH = 22;

			if (isActive)
			{
				int btnStopW = 120;
				int btnStopX = boxX + 8;
				// Click Nút Dừng Xmap
				if (px >= btnStopX && px <= btnStopX + btnStopW && py >= bottomY && py <= bottomY + btnH)
				{
					stopXmap();
					GameCanvas.isPointerJustRelease = false;
					return;
				}

				// Click Nút Goback
				int btnGbW = boxW - btnStopW - 24;
				int btnGbX = btnStopX + btnStopW + 8;
				if (px >= btnGbX && px <= btnGbX + btnGbW && py >= bottomY && py <= bottomY + btnH)
				{
					toggleGoback();
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}
			else
			{
				int btnGbW = 130;
				int btnGbX = boxX + boxW - btnGbW - 8;
				if (px >= btnGbX && px <= btnGbX + btnGbW && py >= bottomY && py <= bottomY + btnH)
				{
					toggleGoback();
					GameCanvas.isPointerJustRelease = false;
					return;
				}
			}
		}
	}

	public static bool onMenuAction(int idAction)
	{
		if (idAction == 12099)
		{
			stopXmap();
			return true;
		}
		if (idAction >= 12100 && idAction <= 12600)
		{
			int targetId = idAction - 12100;
			startXmap(targetId);
			return true;
		}
		return false;
	}
}
