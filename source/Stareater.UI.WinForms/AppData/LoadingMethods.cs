﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stareater.Localization;
using System.IO;

namespace Stareater.AppData
{
	static class LoadingMethods
	{
		#region Localization
		private const string LanguagesFolder = "./languages/";
		private const string DefaultLangSufix = "(default)";
		private static string DefaultLangCode = null;

		public static void InitializeLocalization()
		{
			Language currentLanguage = null;
			Language defaultLanguage = null;
			var infos = new List<LanguageInfo>();

			foreach (var folder in new DirectoryInfo(LanguagesFolder).EnumerateDirectories())
			{
				string code = folder.Name;

				if (folder.Name.EndsWith(DefaultLangSufix, StringComparison.InvariantCultureIgnoreCase))
				{
					code = code.Remove(code.Length - DefaultLangSufix.Length);
					DefaultLangCode = code;
				}
				
				Language lang = LoadLanguage(code);

				if (code == DefaultLangCode)
					defaultLanguage = lang;
				if (code == SettingsWinforms.Get.LanguageId)
					currentLanguage = lang;

				
				infos.Add(new LanguageInfo(code, lang["General"]["LanguageName"].Text()));
			}

			LocalizationManifest.Initialize(infos, defaultLanguage, currentLanguage);
		}

		public static Language LoadLanguage(string langCode)
		{
			var folderSufix = langCode == DefaultLangCode ? DefaultLangSufix : "";

			return new Language(
				langCode,
				dataStreams(new DirectoryInfo(LanguagesFolder + langCode + folderSufix).EnumerateFiles())
			);
		}

		private static IEnumerable<TextReader> dataStreams(IEnumerable<FileInfo> files)
		{
			foreach (var file in files)
			{
				var stream = new StreamReader(file.FullName);
				yield return stream;
				stream.Close();
			}
		}
		#endregion
	}
}
