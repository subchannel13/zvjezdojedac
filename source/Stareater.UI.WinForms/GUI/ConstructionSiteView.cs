﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Stareater.AppData;
using Stareater.Controllers;
using Stareater.Localization;
using Stareater.Utils.Collections;
using Stareater.GameData;

namespace Stareater.GUI
{
	public partial class ConstructionSiteView : UserControl
	{
		private const double MinimumPerTurnDone = 1e-3;
		
		private AConstructionSiteController controller;
		
		public ConstructionSiteView()
		{
			InitializeComponent();
		}
		
		public void SetView(AConstructionSiteController siteController)
		{
			controller = siteController;
			
			industrySlider.Value = (int)(siteController.DesiredSpendingRatio * industrySlider.Maximum);
			
			resetView();
		}
		
		private void resetView()
		{
			this.Font = SettingsWinforms.Get.FormFont;

			Context context = SettingsWinforms.Get.Language["FormMain"];
			this.detailsButton.Text = context["SiteDetails"].Text();
			
			if (controller.ConstructionQueue.Count() == 0) {
				this.queueButton.Text = context["NotBuilding"].Text();
				this.queueButton.Image = null;
				
				industrySlider.Enabled = false;
			}
			else {
				this.queueButton.Text = "";
				this.queueButton.Image = ImageCache.Get[controller.ConstructionQueue.First().ImagePath];
				
				industrySlider.Enabled = !controller.IsReadOnly;
			}
			
			resetEstimation();
		}
		
		private void resetEstimation()
		{
			var constructionItem = controller.ConstructionQueue.FirstOrDefault();
			Context context = SettingsWinforms.Get.Language["FormMain"];
			
			if (constructionItem != null) {
				if (constructionItem.PerTurnDone < MinimumPerTurnDone)
					estimationLabel.Text = context["EtaNever"].Text(null);
				else if (constructionItem.PerTurnDone >= 1) {
					var vars = new Var("count", constructionItem.PerTurnDone.Value).Get;
					estimationLabel.Text = context["BuildingsPerTurn"].Text(vars);
				}
				else {
					var vars = new Var("eta", 1 / constructionItem.PerTurnDone.Value).Get;
					estimationLabel.Text = context["Eta"].Text(vars);
				}
			}
			else
				estimationLabel.Text = "No construction plans";
		}
		
		private void queueButton_Click(object sender, EventArgs e)
		{
			if (controller == null)
				return;
			
			using (var form = new FormBuildingQueue(controller))
				form.ShowDialog();
			
			resetView();
		}

		private void industrySlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (e.Type == ScrollEventType.EndScroll)
				return;
			
			controller.DesiredSpendingRatio = e.NewValue / (double)industrySlider.Maximum;
			resetEstimation();
		}

		private void detailsButton_Click(object sender, EventArgs e)
		{
			Form form = null;

			switch (controller.SiteType) {
				case SiteType.Colony:
					form = new FormColonyDetails(controller as ColonyController);
					break;
				case SiteType.StarSystem:
					form = new FormStellarisDetails(controller as StellarisAdminController);
					break;
			}

			form.ShowDialog();
			form.Dispose();
		}
	}
}
