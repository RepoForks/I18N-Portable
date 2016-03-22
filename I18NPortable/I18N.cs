﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace I18NPortable
{
	public class I18N
	{
		#region Singleton

		public static I18N Current => I18NInstance.Value;

		// Lazy initialization ensures I18NPortableInstance creation is threadsafe
		private static readonly Lazy<I18N> I18NInstance = new Lazy<I18N>(() =>
				new I18N());

		#endregion

		private readonly Dictionary<string, string> _translations = new Dictionary<string, string>();
		private readonly Dictionary<string, string> _locales = new Dictionary<string, string>();
		private Assembly _hostAssembly;
		private bool _logEnabled = true;
		private bool _throwWhenKeyNotFound;
		private string _notFoundSymbol = "?";
		private string _defaultLocale;
		private string _currentLocale;

		private I18N()
		{
			
		}

		#region Fluent API

		public I18N SetNotFoundSymbol(string symbol)
		{
			if (!string.IsNullOrEmpty(symbol))
				_notFoundSymbol = symbol;
			return this;
		}

		public I18N SetLogEnabled(bool enabled)
		{
			_logEnabled = enabled;
			return this;
		}

		public I18N SetThrowWhenKeyNotFound(bool enabled)
		{
			_throwWhenKeyNotFound = enabled;
			return this;
		}

		public I18N SetDefaultLocale(string locale)
		{
			_defaultLocale = locale;
			return this;
		}

		public I18N Init(Assembly hostAssembly)
		{
			DiscoverLocales(hostAssembly);

			_hostAssembly = hostAssembly;

			if (string.IsNullOrEmpty(_defaultLocale))
			{
				_defaultLocale = GetDefaultLocaleFromCurrentCulture();

				if(_defaultLocale != null)
					Log($"Default locale from current culture: {_defaultLocale}");
				else
				{
					_defaultLocale = _locales.Keys.ToArray()[0];
					Log($"No default locale explicitly set. Using the first on the list: {_defaultLocale}");
				}
			}
			else
			{
				Log($"Default locale: {_defaultLocale}");
			}	

			LoadLocale(_defaultLocale);

			return this;
		}

		#endregion

		#region Load stuff

		private void DiscoverLocales(Assembly hostAssembly)
		{
			Log("Getting available locales...");

			var localeResourceNames = hostAssembly
				.GetManifestResourceNames()
				.Where(x => x.Contains("Locales.") && x.EndsWith(".txt"))
				.ToArray();

			if (localeResourceNames.Length == 0)
			{
				throw new Exception("No locales have been found. Make sure you´ve got a folder " +
									"called 'Locales' containing .txt files in the host PCL assembly");
			}

			foreach (var resource in localeResourceNames)
			{
				var parts = resource.Split('.');
				var localeName = parts[parts.Length - 2];
				
				_locales.Add(localeName, resource);
			}

			Log($"Found {localeResourceNames.Length} locales: {string.Join(", ", _locales.Keys.ToArray())}");
		}

		public void LoadLanguage(Language language) => LoadLocale(language.Locale);

		public void LoadLocale(string locale)
		{
			if (!_locales.ContainsKey(locale))
				throw new KeyNotFoundException($"Locale '{locale}' is not available");

			var resourceName = _locales[locale];
			var stream = _hostAssembly.GetManifestResourceStream(resourceName);

			LoadTranslations(stream);

			_currentLocale = locale;
		}

		private void LoadTranslations(Stream stream)
		{
			using (var streamReader = new StreamReader(stream))
			{
				while (!streamReader.EndOfStream)
				{
					var line = streamReader.ReadLine();
					if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
						continue;

					var columns = line.Split(new[] { '=' }, 2);
					_translations.Add(columns[0].Trim(), columns[1].Trim());
				}
			}

			LogTranslations();
		}

		#endregion

		#region Translations

		public string Translate(string key, params object[] args)
		{
			if (_translations.ContainsKey(key))
				return args.Length == 0 ? _translations[key] : string.Format(_translations[key], args);

			if(_throwWhenKeyNotFound)
				throw new KeyNotFoundException($"[{nameof(I18N)}] key '{key}' not found in the current language '{_currentLocale}'");

			return $"{_notFoundSymbol}{key}{_notFoundSymbol}";
		}

		public IEnumerable<Language> GetLanguages() => 
			_locales.Select(x => new Language {Locale = x.Key, DisplayName = Translate(x.Key)}).ToList();

		#endregion

		#region Helpers

		private string GetDefaultLocaleFromCurrentCulture()
		{
			var currentCulture = CultureInfo.CurrentCulture;

			// only available in runtime (not from PCL) // TODO (runtime properties are working in iOS, test other platforms)
			var threeLetterIsoName = currentCulture.GetType().GetRuntimeProperty("ThreeLetterISOLanguageName").GetValue(currentCulture);
			var threeLetterWindowsName = currentCulture.GetType().GetRuntimeProperty("ThreeLetterWindowsLanguageName").GetValue(currentCulture);

			var valuePair = _locales.FirstOrDefault(x => x.Key.Equals(currentCulture.Name) // i.e: "es-ES", "en-US"
				|| x.Key.Equals(currentCulture.TwoLetterISOLanguageName) // ISO 639-1 two-letter code. i.e: "es"
				|| x.Key.Equals(threeLetterIsoName) // ISO 639-2 three-letter code. i.e: "spa"
				|| x.Key.Equals(threeLetterWindowsName)); // "ESP"

			return valuePair.Value;
		}

		#endregion

		#region Logging

		private void LogTranslations()
		{
			if (!_logEnabled) return;

			Log("========== I18N translations ==========");
			foreach (var item in _translations)
				Log($"{item.Key} = {item.Value}");
		}

		private void Log(string trace)
		{
			if (_logEnabled)
				Debug.WriteLine($"[{nameof(I18N)}] {trace}");
		}

		#endregion
	}
}


//namespace I18N
//{
//    public static class I18N
//    {
//		public static string[] SupportedLanguages { get; private set; }
//		public static string LanguageCode { get; private set; }

//		private static Dictionary<string, string> _dictionary;
//		private static Assembly _assembly;

//	    public static void Init(string[] supportedLanguages, string languageToLoad = null, bool logTranslations = false)
//	    {
//			if(supportedLanguages == null || supportedLanguages.Length < 1)
//				throw new ArgumentException("You must provide an array of language codes");

//		    SupportedLanguages = supportedLanguages;

//			if(!string.IsNullOrEmpty(languageToLoad))
//				LoadLanguage(languageToLoad, logTranslations);
//		}

//	    public static void LoadLanguage(string languageCode, bool logTranslations = false)
//	    {
//			if (SupportedLanguages == null || SupportedLanguages.Length < 1)
//				throw new ArgumentException("You must provide an array of language codes");

//			LanguageCode = !SupportedLanguages.Contains(languageCode) ? SupportedLanguages[0] : languageCode;
//			_assembly = _assembly ?? (_assembly = typeof(I18N).GetTypeInfo().Assembly);

//			var bundleResource = $"Locales.{LanguageCode}.txt";
//			var stream = GetResourceStream(bundleResource);
//			if (stream == null) return;

//			LoadDictionary(stream);

//			if (logTranslations)
//				LogTranslations();
//		}

//		private static Stream GetResourceStream(string resourceName) => _assembly.GetManifestResourceStream(resourceName);

//	    public static string Translate(this string key, params object[] args)
//		{
//			var nonTranslated = "?" + key + "?";

//			if (_dictionary == null || !_dictionary.ContainsKey(key)) return nonTranslated;
//			return args.Length == 0 ? _dictionary[key] : string.Format(_dictionary[key], args);
//		}

//		public static string TranslateOrNull(this string key, params object[] args)
//		{
//			if (_dictionary != null && _dictionary.ContainsKey(key))
//			{
//				return string.Format(_dictionary[key], args);
//			}

//			return null;
//		}

//		public static Dictionary<T, string> TranslateEnum<T>()
//		{
//			var type = typeof(T);
//			var dic = new Dictionary<T, string>();

//			foreach (var value in Enum.GetValues(type))
//			{
//				var name = Enum.GetName(type, value);
//				dic.Add((T)value, $"Enums.{type.Name}.{name}".Translate());
//			}

//			return dic;
//		}

//		private static void LogTranslations()
//		{
//			foreach (var item in _dictionary)
//				Debug.WriteLine($"{item.Key} = {item.Value}");
//		}

//		private static void LoadDictionary(Stream stream)
//		{
//			_dictionary = new Dictionary<string, string>();

//			using (var streamReader = new StreamReader(stream))
//			{
//				while (!streamReader.EndOfStream)
//				{
//					var line = streamReader.ReadLine();
//					if (!string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
//					{
//						var columns = line.Split(new char[] { '=' }, 2);
//						_dictionary.Add(columns[0].Trim(), columns[1].Trim());
//					}
//				}
//			}
//		}
//	}
//}
