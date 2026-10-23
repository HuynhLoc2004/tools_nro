public class InputDlg : Dialog
{
	protected string[] info;

	public TField tfInput;

	private int padLeft;

	public InputDlg()
	{
		padLeft = 40;
		if (GameCanvas.w <= 176)
		{
			padLeft = 10;
		}
		tfInput = new TField();
		tfInput.x = padLeft + 10;
		tfInput.y = GameCanvas.h - mScreen.ITEM_HEIGHT - 43;
		tfInput.width = GameCanvas.w - 2 * (padLeft + 10);
		tfInput.height = mScreen.ITEM_HEIGHT + 2;
		tfInput.isFocus = true;
		right = tfInput.cmdClear;
	}

	public void show(string info, Command ok, int type)
	{
		tfInput.setText(string.Empty);
		tfInput.setIputType(type);
		this.info = mFont.tahoma_8b.splitFontArray(info, GameCanvas.w - padLeft * 2);
		left = new Command(mResources.DELETE, GameCanvas.gI(), 8882, null);
		center = ok;
		
		int dlgX = padLeft;
		int dlgY = GameCanvas.h - 82 - mScreen.cmdH;
		int dlgW = GameCanvas.w - padLeft * 2;
		
		tfInput.x = dlgX + 12;
		tfInput.y = dlgY + 20;
		tfInput.width = dlgW - 24;
		tfInput.height = 24;
		
		if (center != null)
		{
			center.w = 78;
			center.h = 23;
			center.x = dlgX + dlgW / 2 - 82;
			center.y = dlgY + 49;
		}
		if (left != null)
		{
			left.w = 78;
			left.h = 23;
			left.x = dlgX + dlgW / 2 + 4;
			left.y = dlgY + 49;
		}
		
		show();
	}

	public override void paint(mGraphics g)
	{
		int dlgX = padLeft;
		int dlgY = GameCanvas.h - 82 - mScreen.cmdH;
		int dlgW = GameCanvas.w - padLeft * 2;
		int dlgH = 78;

		GameCanvas.paintz.paintInputDlg(g, dlgX, dlgY, dlgW, dlgH, info);
		tfInput.paint(g);
		base.paint(g);
	}

	public override void keyPress(int keyCode)
	{
		tfInput.keyPressed(keyCode);
		base.keyPress(keyCode);
	}

	public override void update()
	{
		tfInput.update();
		base.update();
	}

	public override void show()
	{
		GameCanvas.currentDialog = this;
	}

	public void hide()
	{
		GameCanvas.endDlg();
	}
}
