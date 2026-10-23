using System;
using UnityEngine;

public static class UITheme
{
	// Màu sắc nút bấm Gel / Jelly 3D (Hình 1 & 2)
	public const int COLOR_BTN_GREEN_BORDER = 0x145A27;
	public const int COLOR_BTN_GREEN_TOP = 0x4CD964;
	public const int COLOR_BTN_GREEN_BOT = 0x27AE60;
	public const int COLOR_BTN_GREEN_BEVEL = 0x1E8449;

	public const int COLOR_BTN_GOLD_BORDER = 0x6E3800;
	public const int COLOR_BTN_GOLD_TOP = 0xFFB300;
	public const int COLOR_BTN_GOLD_BOT = 0xE67E00;
	public const int COLOR_BTN_GOLD_BEVEL = 0xB85500;

	// Màu sắc Input Box Pastel Mint 3D Inset (Hình 2)
	public const int COLOR_INPUT_BG = 0xC4F2DB;
	public const int COLOR_INPUT_BORDER = 0x367A55;
	public const int COLOR_INPUT_SHADOW = 0x62AA85;
	public const int COLOR_INPUT_HILIGHT = 0xE8FAF2;
	public const int COLOR_INPUT_TEXT = 0x0F291B;

	// Nút xoá màu đỏ (X)
	public const int COLOR_CLEAR_BG = 0x9B1C1C;
	public const int COLOR_CLEAR_BORDER = 0x540C0C;
	public const int COLOR_CLEAR_X = 0xFFFFFF;

	// Hộp thoại Cream / Beige (Hình 2)
	public const int COLOR_DIALOG_BG = 0xFFF9ED;
	public const int COLOR_DIALOG_BORDER = 0xA88352;

	// Khung Chat Bubble trắng (Hình 1)
	public const int COLOR_BUBBLE_BG = 0xFFFFFF;
	public const int COLOR_BUBBLE_BORDER = 0x1A2230;

	/// <summary>
	/// Vẽ hình chữ nhật bo góc đặc (Fill Rounded Rectangle) chuẩn từng pixel, không nhòe
	/// </summary>
	public static void fillRoundRect(mGraphics g, int x, int y, int w, int h, int r)
	{
		if (w <= 0 || h <= 0) return;
		if (r <= 0)
		{
			g.fillRect(x, y, w, h);
			return;
		}
		if (r * 2 > w) r = w / 2;
		if (r * 2 > h) r = h / 2;

		// Thân giữa
		g.fillRect(x + r, y, w - 2 * r, h);
		// Hai bên hông
		g.fillRect(x, y + r, r, h - 2 * r);
		g.fillRect(x + w - r, y + r, r, h - 2 * r);

		// Bo tròn 4 góc
		for (int dy = 0; dy < r; dy++)
		{
			int dyFromCenter = r - 1 - dy;
			int dx = r - (int)System.Math.Round(System.Math.Sqrt(r * r - dyFromCenter * dyFromCenter));
			int segW = r - dx;
			if (segW > 0)
			{
				g.fillRect(x + dx, y + dy, segW, 1);
				g.fillRect(x + w - r, y + dy, segW, 1);
				g.fillRect(x + dx, y + h - 1 - dy, segW, 1);
				g.fillRect(x + w - r, y + h - 1 - dy, segW, 1);
			}
		}
	}

	/// <summary>
	/// Vẽ viền bo góc sắc nét (Draw Rounded Rectangle Border)
	/// </summary>
	public static void drawRoundRect(mGraphics g, int x, int y, int w, int h, int r)
	{
		if (w <= 0 || h <= 0) return;
		if (r <= 0)
		{
			g.drawRect(x, y, w, h);
			return;
		}
		if (r * 2 > w) r = w / 2;
		if (r * 2 > h) r = h / 2;

		// Các cạnh thẳng
		g.fillRect(x + r, y, w - 2 * r, 1);
		g.fillRect(x + r, y + h - 1, w - 2 * r, 1);
		g.fillRect(x, y + r, 1, h - 2 * r);
		g.fillRect(x + w - 1, y + r, 1, h - 2 * r);

		// Góc bo
		for (int dy = 0; dy < r; dy++)
		{
			int dyFromCenter = r - 1 - dy;
			int dx = r - (int)System.Math.Round(System.Math.Sqrt(r * r - dyFromCenter * dyFromCenter));
			g.fillRect(x + dx, y + dy, 1, 1);
			g.fillRect(x + w - 1 - dx, y + dy, 1, 1);
			g.fillRect(x + dx, y + h - 1 - dy, 1, 1);
			g.fillRect(x + w - 1 - dx, y + h - 1 - dy, 1, 1);
		}
	}

	/// <summary>
	/// Vẽ nút thạch 3D bóng bẩy dạng kẹo/gel bo tròn sắc nét (Hình 1)
	/// </summary>
	public static void paintGlossyButton(mGraphics g, int x, int y, int w, int h, bool isFocused, string caption, string[] subCaption = null, int radius = 12)
	{
		if (w <= 0 || h <= 0) return;

		int borderColor = isFocused ? COLOR_BTN_GREEN_BORDER : COLOR_BTN_GOLD_BORDER;
		int topColor = isFocused ? COLOR_BTN_GREEN_TOP : COLOR_BTN_GOLD_TOP;
		int botColor = isFocused ? COLOR_BTN_GREEN_BOT : COLOR_BTN_GOLD_BOT;
		int bevelColor = isFocused ? COLOR_BTN_GREEN_BEVEL : COLOR_BTN_GOLD_BEVEL;

		// 1. Viền ngoài mảnh và sắc nét (1px)
		g.setColor(borderColor);
		fillRoundRect(g, x, y, w, h, radius);

		// 2. Thân nút trong viền
		int innerX = x + 1;
		int innerY = y + 1;
		int innerW = w - 2;
		int innerH = h - 2;
		int innerR = radius > 1 ? radius - 1 : 1;

		// Đáy gờ bevel tạo độ dày 3D
		g.setColor(bevelColor);
		fillRoundRect(g, innerX, innerY, innerW, innerH, innerR);

		// Nửa dưới gradient
		g.setColor(botColor);
		fillRoundRect(g, innerX, innerY, innerW, innerH - 3, innerR);

		// Nửa trên tươi sáng
		g.setColor(topColor);
		fillRoundRect(g, innerX, innerY, innerW, innerH / 2, innerR);

		// 3. Vệt sáng phản chiếu bóng bẩy mỏng nhẹ chuẩn Hình 1 (Không dùng mảng trắng đục)
		if (innerW > 12 && innerH > 8)
		{
			g.setColor(0xFFFFFF, 0.35f);
			g.fillRect(innerX + innerR, innerY + 1, innerW - 2 * innerR, 1);

			// Đốm sáng nhỏ góc trên trái
			g.setColor(0xFFFFFF, 0.50f);
			fillRoundRect(g, innerX + 4, innerY + 3, 4, 3, 1);
		}

		// 4. Chữ hiển thị căn giữa sắc nét - 1 LẦN DUY NHẤT (Không vẽ bóng đè gây nhòe chữ)
		string[] textLines = subCaption;
		if (textLines == null || textLines.Length == 0)
		{
			if (!string.IsNullOrEmpty(caption))
			{
				textLines = new string[] { caption };
			}
		}

		if (textLines != null && textLines.Length > 0)
		{
			int fontH = 14;
			int totalTextH = textLines.Length * fontH;
			int startY = y + (h - totalTextH) / 2 + 1;

			for (int j = 0; j < textLines.Length; j++)
			{
				string line = textLines[j];
				int lineY = startY + j * fontH;
				int lineX = x + w / 2;

				mFont.tahoma_7b_dark.drawString(g, line, lineX, lineY, 2);
			}
		}
	}

	/// <summary>
	/// Vẽ nút con nhộng (Pill Button) bóng bẩy màu vàng hoàng kim sắc nét (Hình 2: OK, Xóa)
	/// </summary>
	public static void paintPillButton(mGraphics g, int x, int y, int w, int h, bool isFocused, string caption)
	{
		if (w <= 0 || h <= 0) return;
		int pillR = h / 2;

		int borderColor = COLOR_BTN_GOLD_BORDER;
		int topColor = isFocused ? 0xFFCE33 : COLOR_BTN_GOLD_TOP;
		int botColor = isFocused ? 0xF58A00 : COLOR_BTN_GOLD_BOT;
		int bevelColor = COLOR_BTN_GOLD_BEVEL;

		// 1. Viền ngoài hình con nhộng bo tròn 2 đầu (1px)
		g.setColor(borderColor);
		fillRoundRect(g, x, y, w, h, pillR);

		// 2. Thân con nhộng
		int innerX = x + 1;
		int innerY = y + 1;
		int innerW = w - 2;
		int innerH = h - 2;
		int innerR = pillR > 1 ? pillR - 1 : 1;

		// Đáy gờ bevel
		g.setColor(bevelColor);
		fillRoundRect(g, innerX, innerY, innerW, innerH, innerR);

		// Nửa dưới
		g.setColor(botColor);
		fillRoundRect(g, innerX, innerY, innerW, innerH - 2, innerR);

		// Nửa trên tươi sáng
		g.setColor(topColor);
		fillRoundRect(g, innerX, innerY, innerW, innerH / 2, innerR);

		// 3. Vệt sáng mỏng tinh tế phía trên
		g.setColor(0xFFFFFF, 0.4f);
		fillRoundRect(g, innerX + 6, innerY + 2, innerW - 12, System.Math.Max(innerH / 4, 3), 2);

		// 4. Chữ căn giữa sắc nét - 1 LẦN DUY NHẤT (Không vẽ bóng đè)
		if (!string.IsNullOrEmpty(caption))
		{
			int textY = y + (h - 14) / 2 + 1;
			mFont.tahoma_7b_dark.drawString(g, caption, x + w / 2, textY, 2);
		}
	}

	/// <summary>
	/// Vẽ ô nhập dữ liệu Pastel Mint phong cách 3D chìm (Inset) sắc nét (Hình 2)
	/// </summary>
	public static void paintMintInputBox(mGraphics g, int x, int y, int w, int h, bool isFocused, string text, string placeholder, int xText, int yText)
	{
		if (w <= 0 || h <= 0) return;
		int r = 8;

		// 1. Viền ngoài màu xanh rêu đậm
		g.setColor(COLOR_INPUT_BORDER);
		fillRoundRect(g, x, y, w, h, r);

		// 2. Nền xanh ngọc Pastel Mint tươi mát (#C4F2DB)
		g.setColor(COLOR_INPUT_BG);
		fillRoundRect(g, x + 1, y + 1, w - 2, h - 2, r - 1);

		// 3. Hiệu ứng lõm 3D Inset: Đường bóng mỏng trên và trái
		g.setColor(COLOR_INPUT_SHADOW);
		g.fillRect(x + r, y + 1, w - 2 * r, 1);
		g.fillRect(x + 1, y + r, 1, h - 2 * r);

		// 4. Đường phản quang dưới và phải
		g.setColor(COLOR_INPUT_HILIGHT);
		g.fillRect(x + r, y + h - 2, w - 2 * r, 1);
		g.fillRect(x + w - 2, y + r, 1, h - 2 * r);

		// 5. Vẽ chữ văn bản sắc nét
		g.setClip(x + 8, y + 1, w - 38, h - 2);
		if (!string.IsNullOrEmpty(text))
		{
			mFont.tahoma_8b.drawString(g, text, xText, yText, 0);
		}
		else if (!string.IsNullOrEmpty(placeholder))
		{
			if (isFocused)
			{
				mFont.tahoma_7b_focus.drawString(g, placeholder, xText, yText, 0);
			}
			else
			{
				mFont.tahoma_7b_unfocus.drawString(g, placeholder, xText, yText, 0);
			}
		}
	}

	/// <summary>
	/// Vẽ nút xoá tròn đỏ (X) sắc nét ở bên phải ô nhập (Hình 2)
	/// </summary>
	public static void paintClearIcon(mGraphics g, int cx, int cy)
	{
		int r = 8;
		int x = cx - r;
		int y = cy - r;
		int d = r * 2;

		// 1. Viền ngoài hình tròn màu đỏ thẫm
		g.setColor(COLOR_CLEAR_BORDER);
		fillRoundRect(g, x, y, d, d, r);

		// 2. Thân tròn màu đỏ nhung nổi bật (#9B1C1C)
		g.setColor(COLOR_CLEAR_BG);
		fillRoundRect(g, x + 1, y + 1, d - 2, d - 2, r - 1);

		// 3. Chữ X màu trắng tinh khôi ở chính giữa
		g.setColor(COLOR_CLEAR_X);
		g.fillRect(cx - 3, cy - 3, 2, 2);
		g.fillRect(cx - 1, cy - 1, 2, 2);
		g.fillRect(cx + 1, cy + 1, 2, 2);
		g.fillRect(cx + 1, cy - 3, 2, 2);
		g.fillRect(cx - 3, cy + 1, 2, 2);
	}

	/// <summary>
	/// Khung thoại NPC bong bóng trắng bo góc sắc nét (Hình 1) - Không đổ bóng mờ gây nhòe
	/// </summary>
	public static void paintWhiteSpeechBubble(mGraphics g, int x, int y, int w, int h)
	{
		int r = 10;
		// 1. Viền ngoài đậm nét chuẩn mực (1.5px)
		g.setColor(COLOR_BUBBLE_BORDER);
		fillRoundRect(g, x, y, w, h, r);

		// 2. Nền trắng tinh khiết sáng rõ
		g.setColor(COLOR_BUBBLE_BG);
		fillRoundRect(g, x + 1, y + 1, w - 2, h - 2, r - 1);
	}

	/// <summary>
	/// Khung thoại Cream / Vàng cát bo góc (Hình 2) - Không đổ bóng mờ gây nhòe
	/// </summary>
	public static void paintCreamDialog(mGraphics g, int x, int y, int w, int h)
	{
		int r = 10;
		// 1. Viền ngoài tông nâu ấm sang trọng
		g.setColor(COLOR_DIALOG_BORDER);
		fillRoundRect(g, x, y, w, h, r);

		// 2. Nền màu kem sữa (Cream / Ivory #FFF9ED)
		g.setColor(COLOR_DIALOG_BG);
		fillRoundRect(g, x + 2, y + 2, w - 4, h - 4, r - 2);
	}

	/// <summary>
	/// Ô vật phẩm / Trang bị 3D Inset nổi bật
	/// </summary>
	public static void paintItemSlot3D(mGraphics g, int x, int y, int w, int h, bool isSelected)
	{
		g.setColor(isSelected ? 0xFFA500 : 0x2B180A);
		g.fillRect(x, y, w, h);

		g.setColor(0x422B18);
		g.fillRect(x + 1, y + 1, w - 2, h - 2);

		g.setColor(0x1F1206);
		g.fillRect(x + 1, y + 1, w - 2, 1);
		g.fillRect(x + 1, y + 1, 1, h - 2);

		g.setColor(0x69472B);
		g.fillRect(x + 1, y + h - 2, w - 2, 1);
		g.fillRect(x + w - 2, y + 1, 1, h - 2);

		if (isSelected)
		{
			g.setColor(0xFFF380, 0.5f);
			g.drawRect(x + 1, y + 1, w - 3, h - 3);
		}
	}

	/// <summary>
	/// Vẽ tab bo tròn phong cách Gel / Cream sắc nét (Panel hành trang)
	/// </summary>
	public static void paintTabButton(mGraphics g, int x, int y, int w, int h, bool isFocused)
	{
		int r = 5;
		if (isFocused)
		{
			g.setColor(COLOR_BTN_GREEN_BORDER);
			fillRoundRect(g, x, y, w, h, r);
			g.setColor(COLOR_BTN_GREEN_BOT);
			fillRoundRect(g, x + 1, y + 1, w - 2, h - 2, r - 1);
			g.setColor(COLOR_BTN_GREEN_TOP);
			fillRoundRect(g, x + 1, y + 1, w - 2, (h - 2) / 2, r - 1);
			g.setColor(0xFFFFFF, 0.4f);
			g.fillRect(x + 3, y + 2, w - 6, 1);
		}
		else
		{
			g.setColor(COLOR_DIALOG_BORDER);
			fillRoundRect(g, x, y, w, h, r);
			g.setColor(COLOR_DIALOG_BG);
			fillRoundRect(g, x + 1, y + 1, w - 2, h - 2, r - 1);
		}
	}

	/// <summary>
	/// Vẽ khung thông tin nhân vật ở trên Panel hành trang
	/// </summary>
	public static void paintInsetHeader(mGraphics g, int x, int y, int w, int h)
	{
		g.setColor(COLOR_INPUT_BORDER);
		fillRoundRect(g, x, y, w, h, 6);
		g.setColor(COLOR_INPUT_BG);
		fillRoundRect(g, x + 1, y + 1, w - 2, h - 2, 5);
		g.setColor(COLOR_INPUT_SHADOW);
		g.fillRect(x + 5, y + 1, w - 10, 1);
		g.fillRect(x + 1, y + 5, 1, h - 10);
	}
}
