﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Stareater.Maps;
using Stareater.Utils.PluginParameters;

namespace Stareater.GUI
{
	public partial class MapParameterSelector : UserControl
	{
		SelectorParameter parameter;

		public MapParameterSelector()
		{
			InitializeComponent();
		}

		public void SetData(SelectorParameter parameterInfo)
		{
			this.parameter = parameterInfo;
			nameLabel.Text = parameterInfo.Name;

			foreach (var valueInfo in parameterInfo)
				valueSelector.Items.Add(new Tag<int>(valueInfo.Key, valueInfo.Value));

			valueSelector.SelectedIndex = 0;
		}

		private void valueSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			parameter.Value = valueSelector.SelectedIndex;
		}
	}
}
