﻿using OpenTK;
using Stareater.GLData;
using Stareater.Utils;
using System;
using System.Collections.Generic;

namespace Stareater.GraphicsEngine.GuiElements
{
	class ElementPosition
	{
		public Vector2 Center { get; private set; }
		public Vector2 Size { get; private set; }
		public ClipWindow ClipArea { get; private set; }

		private readonly List<IPositioner> positioners = new List<IPositioner>();
		private readonly Func<Vector2> contentSize;

		public ElementPosition(Func<Vector2> contentSize)
		{
			this.contentSize = contentSize;
			this.ClipArea = new ClipWindow();
		}

		public void Recalculate(ElementPosition parentPosition)
		{
			foreach (var positioner in this.positioners)
				positioner.Recalculate(this, parentPosition);

			if (parentPosition != null)
				this.ClipArea = new ClipWindow(this.Center, this.Size, parentPosition.ClipArea);
			else
				this.ClipArea = new ClipWindow(this.Center, this.Size);
		}

		public IEnumerable<AGuiElement> Dependencies
		{
			get
			{
				foreach (var positioner in this.positioners)
					foreach (var element in positioner.Dependencies)
						yield return element;
			}
		}

		#region Position builders
		public ElementPosition FixedSize(float width, float height)
		{
			this.Size = new Vector2(width, height);

			return this;
		}

		public ElementPosition FixedCenter(float x, float y)
		{
			this.Center = new Vector2(x, y);

			return this;
		}

		public ElementPosition ParentRelative(float x, float y, float marginX, float marginY)
		{
			this.positioners.Add(new ParentRelativePositioner(x, y, marginX, marginY));

			return this;
		}

		public ElementPosition WrapContent()
		{
			this.positioners.Add(new WrapContentPositioner(0, 0));

			return this;
		}

		public ElementPosition WrapContent(float marginX, float marginY)
		{
			this.positioners.Add(new WrapContentPositioner(marginX, marginY));

			return this;
		}

		public ElementPosition RelativeTo(AGuiElement anchor, float xPortionAnchor, float yPortionAnchor, float xPortionThis, float yPortionThis, float marginX, float marginY)
		{
			this.positioners.Add(new RelativeToPositioner(anchor, xPortionAnchor, yPortionAnchor, xPortionThis, yPortionThis, marginX, marginY));

			return this;
		}

		public ElementPosition StretchRightTo(AGuiElement anchor, float xPortionAnchor, float marginX)
		{
			this.positioners.Add(new StretchRightToPositioner(anchor, xPortionAnchor, marginX));

			return this;
		}

		public ElementPosition TooltipNear(Vector2 sourcePoint, float sourceMargin, float parentMargin)
		{
			this.positioners.Add(new TooltipPositioner(sourcePoint, sourceMargin, parentMargin));

			return this;
		}
		#endregion

		private interface IPositioner
		{
			void Recalculate(ElementPosition element, ElementPosition parentPosition);
			IEnumerable<AGuiElement> Dependencies { get; }
		}

		private class ParentRelativePositioner : IPositioner
		{
			private readonly float xPortion;
			private readonly float yPortion;
			private readonly float marginX;
			private readonly float marginY;

			public ParentRelativePositioner(float x, float y, float marginX, float marginY)
			{
				this.marginX = marginX;
				this.marginY = marginY;
				this.xPortion = x;
				this.yPortion = y;
			}

			public void Recalculate(ElementPosition element, ElementPosition parentPosition)
			{
				float windowX = parentPosition.Size.X / 2 - marginX - element.Size.X / 2;
				float windowY = parentPosition.Size.Y / 2 - marginY - element.Size.Y / 2;

				element.Center = new Vector2(
					windowX * this.xPortion + parentPosition.Center.X,
					windowY * this.yPortion + parentPosition.Center.Y
				);
			}

			public IEnumerable<AGuiElement> Dependencies
			{
				get { yield break; }
			}
		}

		private class RelativeToPositioner : IPositioner
		{
			private readonly AGuiElement anchor;
			private readonly float xPortionAnchor;
			private readonly float yPortionAnchor;
			private readonly float xPortionThis;
			private readonly float yPortionThis;
			private readonly float marginX;
			private readonly float marginY;

			public RelativeToPositioner(AGuiElement anchor, float xPortionAnchor, float yPortionAnchor, float xPortionThis, float yPortionThis, float marginX, float marginY)
			{
				this.anchor = anchor;
				this.xPortionAnchor = xPortionAnchor;
				this.yPortionAnchor = yPortionAnchor;
				this.xPortionThis = xPortionThis;
				this.yPortionThis = yPortionThis;
				this.marginX = marginX;
				this.marginY = marginY;
			}

			public void Recalculate(ElementPosition element, ElementPosition parentPosition)
			{
				element.Center = new Vector2(
					this.anchor.Position.Center.X + (this.anchor.Position.Size.X + this.marginX) * this.xPortionAnchor / 2 - element.Size.X * this.xPortionThis / 2,
					this.anchor.Position.Center.Y + (this.anchor.Position.Size.Y + this.marginY) * this.yPortionAnchor / 2 - element.Size.Y * this.yPortionThis / 2
				);
			}

			public IEnumerable<AGuiElement> Dependencies
			{
				get { yield return this.anchor; }
			}
		}

		private class StretchRightToPositioner : IPositioner
		{
			private readonly AGuiElement anchor;
			private readonly float xPortionAnchor;
			private readonly float marginX;

			public StretchRightToPositioner(AGuiElement anchor, float xPortionAnchor, float marginX)
			{
				this.anchor = anchor;
				this.xPortionAnchor = xPortionAnchor;
				this.marginX = marginX;
			}

			public void Recalculate(ElementPosition element, ElementPosition parentPosition)
			{
				var widthDelta = this.anchor.Position.Center.X + xPortionAnchor * (this.anchor.Position.Size.X / 2 - this.marginX) -
					(element.Center.X + element.Size.X / 2);

				element.Center = new Vector2(element.Center.X + widthDelta / 2, element.Center.Y);
				element.Size = new Vector2(element.Size.X + widthDelta, element.Size.Y);
			}

			public IEnumerable<AGuiElement> Dependencies
			{
				get { yield return this.anchor; }
			}
		}

		private class TooltipPositioner : IPositioner
		{
			private readonly Vector2 sourcePoint;
			private readonly float sourceMargin;
			private readonly float parentMargin;

			public TooltipPositioner(Vector2 sourcePoint, float sourceMargin, float parentMargin)
			{
				this.sourcePoint = sourcePoint;
				this.sourceMargin = sourceMargin;
				this.parentMargin = parentMargin;
			}

			public void Recalculate(ElementPosition element, ElementPosition parentPosition)
			{
				var leftBound = -parentPosition.Size.X / 2 + this.parentMargin;
				var rigthBound = Math.Max(parentPosition.Size.X / 2 - this.parentMargin - element.Size.X / 2, leftBound);

				element.Center = new Vector2(
					Methods.Clamp(this.sourcePoint.X + element.Size.X / 2, leftBound, rigthBound) + parentPosition.Center.X,
					this.sourcePoint.Y + element.Size.Y / 2 + this.sourceMargin
				);
			}

			public IEnumerable<AGuiElement> Dependencies
			{
				get { yield break; }
			}
		}

		private class WrapContentPositioner : IPositioner
		{
			private readonly float marginX;
			private readonly float marginY;

			public WrapContentPositioner(float marginX, float marginY)
			{
				this.marginX = marginX;
				this.marginY = marginY;
			}

			public void Recalculate(ElementPosition element, ElementPosition parentPosition)
			{
				element.Size = element.contentSize() + new Vector2(this.marginX, this.marginY);
			}

			public IEnumerable<AGuiElement> Dependencies
			{
				get { yield break; }
			}
		}
	}
}
