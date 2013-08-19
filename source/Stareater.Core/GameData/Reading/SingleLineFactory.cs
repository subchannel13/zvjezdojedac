﻿using System;
using Ikadn;

namespace Stareater.GameData.Reading
{
	public class SingleLineFactory : IIkadnObjectFactory
	{
		public IkadnBaseObject Parse(IkadnParser parser)
		{
			return new SingleLineText(parser.Reader.ReadUntil('\n', '\r', IkadnReader.EndOfStreamResult).Trim());
		}

		public char Sign
		{
			get { return ':'; }
		}
	}
}
