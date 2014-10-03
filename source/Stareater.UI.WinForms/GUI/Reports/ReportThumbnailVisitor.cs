﻿using System;
using System.Drawing;
using Stareater.AppData;
using Stareater.Controllers.Data;

namespace Stareater.GUI.Reports
{
	public class ReportThumbnailVisitor : IReportInfoVisitor
	{
		public Image Result { get; private set; }
		
		public ReportThumbnailVisitor(IReportInfo report)
		{
			this.Result = null;
			
			report.Accept(this);
		}
		
		public void Visit(TechnologyReportInfo reportInfo)
		{
			this.Result = ImageCache.Get[reportInfo.ImagePath];
		}
	}
}
