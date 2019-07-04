﻿using Stareater.GLData;
using Stareater.Localization;
using System;
using System.Drawing;

namespace Stareater.GraphicsEngine.GuiElements
{
	class DynamicTooltip : ITooltip
	{
		private readonly string context;
		private readonly Func<string> textKey;

		public DynamicTooltip(string context, Func<string> textKey)
		{
			this.context = context;
			this.textKey = textKey;
		}

		public AGuiElement Make()
		{
			var text = new GuiText()
			{
				Text = LocalizationManifest.Get.CurrentLanguage[this.context][this.textKey()].Text(),
				TextColor = Color.White,
				TextSize = 9,
				MasksMouseClick = false
			};
			text.Position.WrapContent().WithPadding(10, 5).ParentRelative(0, 0);

			var panel = new GuiPanel()
			{
				Background = new BackgroundTexture(GalaxyTextures.Get.PanelBackground, 3),
				MasksMouseClick = false
			};
			panel.AddChild(text);
			panel.Position.WrapContent();

			return panel;
		}
	}
}
