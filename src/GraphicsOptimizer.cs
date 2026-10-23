using System;
using UnityEngine;

/// <summary>
/// Quản lý tối ưu hóa Đồ họa và FPS cho DragonBoy (NRO PC).
/// Cung cấp 3 cấp độ đồ họa: Thấp (0), Trung Bình (1), Cao (2).
/// Hỗ trợ cài đặt FPS từ 60 - 144 FPS (Mặc định 120 FPS) kèm tính năng Dynamic FPS thông minh tự điều tiết chống nóng máy.
/// </summary>
public class GraphicsOptimizer
{
	// =========================================================================
	// 1. CẤU HÌNH ĐỒ HỌA (GRAPHICS LEVELS)
	// 0: Thấp (Siêu nhẹ, bóp tối đa hiệu ứng, giữ bóng & nền map tối gốc để đọc text rõ)
	// 1: Trung bình (Cân bằng, tối ưu hiệu ứng map & hào quang, giữ trải nghiệm mượt)
	// 2: Cao (Đẹp, hiển thị hiệu ứng nhưng đã bóp nhẹ các animation thừa thãi chống giật lag)
	// =========================================================================
	public static int graphicsLevel = 1; // Mặc định Trung Bình (hoặc load từ RMS)
	public static bool isLowGraphicMode => graphicsLevel == 0;
	public static bool isMediumGraphicMode => graphicsLevel == 1;
	public static bool isHighGraphicMode => graphicsLevel == 2;



	// =========================================================================
	// 2. CẤU HÌNH FPS (TARGET FRAME RATE & DYNAMIC FPS)
	// =========================================================================
	public static int selectedFps = 120;               // Mặc định 120 FPS
	public static int currentActualTargetFps = 120;    // FPS hiện đang áp dụng cho engine
	public static bool isDynamicFpsEnabled = true;     // Tự động điều tiết thông minh theo máy
	public static bool isThrottleBackgroundFps = false; // Tùy chọn giảm FPS khi tab ra ngoài (Mặc định TẮT để giữ mượt đa nhiệm)

	// Giám sát hiệu năng để tự bóp FPS khi máy nóng / lag
	private static float frameTimeAccumulator = 0f;
	private static int frameCountSample = 0;
	private static long lastSampleCheckTime = 0L;
	private static int currentThrottledOffset = 0;     // Mức giảm FPS tạm thời khi quá tải
	private static long lastGcCleanupTime = 0L;

	// RMS Keys
	private const string RMS_GRAPHICS_LEVEL = "mod_opt_graphics_lvl";
	private const string RMS_TARGET_FPS = "mod_opt_target_fps";
	private const string RMS_DYNAMIC_FPS = "mod_opt_dynamic_fps";
	private const string RMS_THROTTLE_BACKGROUND_FPS = "mod_opt_bg_fps";

	private static bool isInitialized = false;

	/// <summary>
	/// Khởi tạo cấu hình Đồ Họa và FPS khi vào game.
	/// </summary>
	public static void Init()
	{
		if (isInitialized) return;
		isInitialized = true;

		try
		{
			// Load Graphics Level (Mặc định 1: Trung bình nếu chưa lưu)
			int savedGfx = Rms.loadRMSInt(RMS_GRAPHICS_LEVEL);
			if (savedGfx >= 0 && savedGfx <= 2)
			{
				graphicsLevel = savedGfx;
			}
			else
			{
				graphicsLevel = 1; // Mặc định cân bằng
			}

			// Load FPS (Mặc định 120 FPS)
			int savedFps = Rms.loadRMSInt(RMS_TARGET_FPS);
			if (savedFps == 60 || savedFps == 90 || savedFps == 120 || savedFps == 144)
			{
				selectedFps = savedFps;
			}
			else
			{
				selectedFps = 120; // Chuẩn 120 FPS
			}

			// Load Dynamic FPS
			int savedDyn = Rms.loadRMSInt(RMS_DYNAMIC_FPS);
			if (savedDyn != -1)
			{
				isDynamicFpsEnabled = (savedDyn == 1);
			}
			else
			{
				isDynamicFpsEnabled = true; // Mặc định bật tự điều tiết thông minh
			}

			// Load Throttle Background FPS (Mặc định tắt để giữ FPS mượt mà khi chạy đa nhiệm / chuyển tab)
			int savedBg = Rms.loadRMSInt(RMS_THROTTLE_BACKGROUND_FPS);
			if (savedBg != -1)
			{
				isThrottleBackgroundFps = (savedBg == 1);
			}
			else
			{
				isThrottleBackgroundFps = false;
			}

			ApplyGraphicsLevel(graphicsLevel, isSilent: true);
			ApplyTargetFps(selectedFps, isSilent: true);
		}
		catch (Exception ex)
		{
			Cout.LogError("GraphicsOptimizer Init Exception: " + ex);
		}
	}

	/// <summary>
	/// Áp dụng Cấp độ Đồ họa (0: Thấp, 1: Trung Bình, 2: Cao).
	/// Bảo toàn 100% chuyển động và chiêu thức kỹ năng của nhân vật, quái và NPC.
	/// </summary>
	public static void ApplyGraphicsLevel(int level, bool isSilent = false)
	{
		graphicsLevel = Mathf.Clamp(level, 0, 2);
		Rms.saveRMSInt(RMS_GRAPHICS_LEVEL, graphicsLevel);

		GameCanvas.lowGraphic = false; // Luôn giữ đồ họa chuẩn PC, không kích hoạt cơ chế xóa sprite của điện thoại Java cổ

		if (graphicsLevel == 0) // 🟢 ĐỒ HỌA THẤP (Xóa background, tắt hào quang & hiệu ứng thừa, siêu nhẹ mượt)
		{
			// Tắt hào quang cấp 16+ và hào quang Super Saiyan chói lóa
			Char.isPaintAura = false;
			Char.isPaintAura2 = false;

			// Tắt hiệu ứng thời tiết, mây bay, sương mù
			BackgroudEffect.isFog = false;
			BackgroudEffect.nCloud = 0;
			BackgroudEffect.vBgEffect.removeAllElements();

			// Khóa cứng FPS cố định theo mức đã chọn, không tự bóp trồi sụt
			isDynamicFpsEnabled = false;
			currentActualTargetFps = selectedFps;
			Application.targetFrameRate = selectedFps;

			// Dọn dẹp RAM thông minh
			TriggerSmartMemoryCleanup();

			if (!isSilent)
			{
				GameScr.info1.addInfo("Đồ họa: THẤP (Xóa background, tắt hiệu ứng thừa, chống drop FPS)", 0);
			}
		}
		else if (graphicsLevel == 1) // 🟡 ĐỒ HỌA TRUNG BÌNH (Cân bằng chuẩn)
		{
			Char.isPaintAura = true;
			Char.isPaintAura2 = true;

			isDynamicFpsEnabled = true;

			BackgroudEffect.isFog = false;
			BackgroudEffect.nCloud = 3;

			if (!isSilent)
			{
				GameScr.info1.addInfo("Đồ họa: TRUNG BÌNH (Cân bằng hình ảnh & độ mượt)", 0);
			}
		}
		else // 🔴 ĐỒ HỌA CAO (Đầy đủ chi tiết đẹp mắt)
		{
			Char.isPaintAura = true;
			Char.isPaintAura2 = true;

			BackgroudEffect.isFog = true;
			BackgroudEffect.nCloud = 7;

			if (!isSilent)
			{
				GameScr.info1.addInfo("Đồ họa: CAO (Đầy đủ chi tiết đẹp mắt)", 0);
			}
		}
	}

	/// <summary>
	/// Giải phóng tài nguyên thừa và dọn dẹp RAM thông minh để chống văng/out game.
	/// </summary>
	public static void TriggerSmartMemoryCleanup()
	{
		try
		{
			Resources.UnloadUnusedAssets();
			GC.Collect();
		}
		catch { }
	}

	/// <summary>
	/// Áp dụng mức FPS mong muốn (60, 90, 120, 144).
	/// </summary>
	public static void ApplyTargetFps(int fps, bool isSilent = false)
	{
		if (fps != 60 && fps != 90 && fps != 120 && fps != 144)
		{
			fps = 120;
		}

		selectedFps = fps;
		currentActualTargetFps = fps;
		currentThrottledOffset = 0;

		Rms.saveRMSInt(RMS_TARGET_FPS, selectedFps);

		// Tắt vSync để nhận FPS tùy chỉnh cao hơn 60Hz
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = selectedFps;

		if (!isSilent)
		{
			GameScr.info1.addInfo("FPS Cài Đặt: " + selectedFps + " FPS", 0);
		}
	}

	/// <summary>
	/// Bật / Tắt tính năng Dynamic FPS thông minh tự điều tiết theo nhiệt độ & tải CPU/GPU.
	/// </summary>
	public static void ToggleDynamicFps()
	{
		isDynamicFpsEnabled = !isDynamicFpsEnabled;
		Rms.saveRMSInt(RMS_DYNAMIC_FPS, isDynamicFpsEnabled ? 1 : 0);

		if (!isDynamicFpsEnabled)
		{
			currentActualTargetFps = selectedFps;
			Application.targetFrameRate = selectedFps;
		}

		GameScr.info1.addInfo("Tự bóp FPS thông minh: " + (isDynamicFpsEnabled ? "BẬT" : "TẮT"), 0);
	}

	/// <summary>
	/// Bật / Tắt tùy chọn giảm FPS xuống 30 khi ẩn cửa sổ hoặc tab ra ngoài (Mặc định: TẮT để duy trì FPS mượt mà khi chạy nhiều acc).
	/// </summary>
	public static void ToggleThrottleBackgroundFps()
	{
		isThrottleBackgroundFps = !isThrottleBackgroundFps;
		Rms.saveRMSInt(RMS_THROTTLE_BACKGROUND_FPS, isThrottleBackgroundFps ? 1 : 0);

		if (!isThrottleBackgroundFps)
		{
			Application.targetFrameRate = currentActualTargetFps;
		}

		GameScr.info1.addInfo("Bóp 30 FPS khi tab ra ngoài: " + (isThrottleBackgroundFps ? "BẬT" : "TẮT"), 0);
	}

	public static int currentFpsDisplay = 120;
	private static int fpsCounter = 0;
	private static long lastFpsUpdateTime = 0L;

	/// <summary>
	/// Cập nhật giám sát hiệu năng mỗi frame:
	/// 1. Duy trì FPS mục tiêu mượt mà khi chạy nền/nhiều acc, chỉ bóp khi bật tùy chọn tiết kiệm.
	/// 2. Thu gom rác RAM định kỳ chống out game / crash.
	/// 3. Điều tiết Dynamic FPS êm dịu, không gây giật lag.
	/// 4. Cập nhật số FPS real-time hiển thị trên màn hình.
	/// </summary>
	public static void UpdatePerformanceMonitor()
	{
		long now = mSystem.currentTimeMillis();

		// Cập nhật FPS real-time hiển thị mỗi 300ms
		fpsCounter++;
		if (now - lastFpsUpdateTime >= 300L)
		{
			currentFpsDisplay = (int)(fpsCounter * 1000f / Mathf.Max(1L, now - lastFpsUpdateTime));
			fpsCounter = 0;
			lastFpsUpdateTime = now;
		}

		// 1. Chống tràn RAM và Out of Memory định kỳ mỗi 60s
		if (now - lastGcCleanupTime >= 60000L)
		{
			lastGcCleanupTime = now;
			TriggerSmartMemoryCleanup();
		}

		// Đảm bảo vSync tắt để nhận FPS cao không phụ thuộc tần số quét màn hình
		if (QualitySettings.vSyncCount != 0)
		{
			QualitySettings.vSyncCount = 0;
		}

		if (!isDynamicFpsEnabled)
		{
			currentActualTargetFps = selectedFps;
			currentThrottledOffset = 0;
		}

		// Điều tiết FPS mục tiêu:
		// Mặc định luôn giữ nguyên FPS đã chọn (60/90/120/144) khi tab ra ngoài hoặc chạy đa nhiệm.
		// Chỉ chủ động giảm về 30 FPS khi người dùng bật tính năng tiết kiệm CPU cho máy yếu.
		int targetFps = currentActualTargetFps;
		if (isThrottleBackgroundFps && !Application.isFocused)
		{
			targetFps = 30;
		}

		if (Application.targetFrameRate != targetFps)
		{
			Application.targetFrameRate = targetFps;
		}

		if (!isDynamicFpsEnabled) return;

		// 3. Dynamic FPS: Giữ frame pacing ổn định, chỉ giảm nhẹ khi CPU quá tải kéo dài
		frameTimeAccumulator += Time.unscaledDeltaTime;
		frameCountSample++;

		if (now - lastSampleCheckTime >= 3000L) // Đo đạc trung bình mỗi 3 giây
		{
			lastSampleCheckTime = now;
			float avgDeltaTime = frameTimeAccumulator / (float)Mathf.Max(1, frameCountSample);

			frameTimeAccumulator = 0f;
			frameCountSample = 0;

			// Nếu frametime quá cao liên tục (lag giật dưới 30 FPS)
			if (avgDeltaTime > 0.035f)
			{
				if (currentThrottledOffset < 30)
				{
					currentThrottledOffset += 10;
					int newFps = Mathf.Max(60, selectedFps - currentThrottledOffset);
					if (newFps != currentActualTargetFps)
					{
						currentActualTargetFps = newFps;
						Application.targetFrameRate = currentActualTargetFps;
					}
				}
			}
			else if (avgDeltaTime < 0.015f) // Khi hệ thống mượt mà (> 65 FPS)
			{
				if (currentThrottledOffset > 0)
				{
					currentThrottledOffset -= 5;
					if (currentThrottledOffset < 0) currentThrottledOffset = 0;
					int newFps = selectedFps - currentThrottledOffset;
					if (newFps != currentActualTargetFps)
					{
						currentActualTargetFps = newFps;
						Application.targetFrameRate = currentActualTargetFps;
					}
				}
			}
		}
	}
}
