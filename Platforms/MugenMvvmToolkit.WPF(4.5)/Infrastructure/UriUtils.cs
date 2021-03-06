﻿#region Copyright
// ****************************************************************************
// <copyright file="UriUtils.cs">
// Copyright © Vyacheslav Volkov 2012-2014
// </copyright>
// ****************************************************************************
// <author>Vyacheslav Volkov</author>
// <email>vvs0205@outlook.com</email>
// <project>MugenMvvmToolkit</project>
// <web>https://github.com/MugenMvvmToolkit/MugenMvvmToolkit</web>
// <license>
// See license.txt in this solution or http://opensource.org/licenses/MS-PL
// </license>
// ****************************************************************************
#endregion
using System;
using System.Collections.Generic;
using System.Linq;

namespace MugenMvvmToolkit.Infrastructure
{
    internal static class UriUtils
    {
        #region Fields

        private static readonly Dictionary<string, string> EmptyUriDictionary;

        #endregion

        #region Constructors

        static UriUtils()
        {
            EmptyUriDictionary = new Dictionary<string, string>();
        }

        #endregion

        #region Methods

        internal static Uri MergeUri(this Uri uri, ICollection<KeyValuePair<string, string>> uriParameters)
        {
            if (uriParameters == null)
                return uri;
            Should.NotBeNull(uri, "uri");
            return BuildQueryString(uri, uriParameters);
        }

        internal static IDictionary<string, string> GetUriParameters(this Uri uri)
        {
            return ParseQueryStringToDictionary(uri);
        }

        private static IDictionary<string, string> ParseQueryStringToDictionary(Uri uri)
        {
            if (uri == null)
                return EmptyUriDictionary;
            var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string url in uri.MakeAbsolute()
                .GetComponents(UriComponents.Query, UriFormat.SafeUnescaped)
                .Split("&".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                int length = url.IndexOf("=", StringComparison.Ordinal);
                if (length == -1)
                    dictionary.Add(UrlDecode(url), string.Empty);
                else
                    dictionary.Add(UrlDecode(url.Substring(0, length)), UrlDecode(url.Substring(length + 1)));
            }
            return dictionary;
        }

        private static Uri BuildQueryString(Uri uri, ICollection<KeyValuePair<string, string>> queryString)
        {
            if (queryString.Count < 1)
                return uri;

            IDictionary<string, string> dictionary = ParseQueryStringToDictionary(uri);
            if (dictionary.Count == 0)
                dictionary = new Dictionary<string, string>();
            foreach (var param in queryString)
                dictionary[param.Key] = param.Value;

            string result = dictionary
                .Aggregate("?", (current, pair) => current + (pair.Key + "=" + Uri.EscapeDataString(pair.Value) + "&"));
            result = result.Remove(result.Length - 1);
            if (uri.IsAbsoluteUri)
                return new Uri(uri.OriginalString.Replace(uri.Query, result));
            int indexOf = uri.OriginalString.IndexOf('?');
            if (indexOf == -1)
                indexOf = uri.OriginalString.Length;
            return new Uri(uri.OriginalString.Substring(0, indexOf) + result, UriKind.Relative);
        }

        private static string UrlDecode(string value)
        {
#if SILVERLIGHT && !WINDOWS_PHONE
            return System.Windows.Browser.HttpUtility.UrlDecode(value);
#else
            return Uri.UnescapeDataString(value);
#endif
        }

        internal static Uri MakeAbsolute(this Uri baseUri)
        {
            if (baseUri != null && baseUri.IsAbsoluteUri)
                return baseUri;
            if (baseUri == null || baseUri.OriginalString.StartsWith("/", StringComparison.Ordinal))
                return new Uri("http://localhost" + baseUri, UriKind.Absolute);
            return new Uri("http://localhost/" + baseUri, UriKind.Absolute);
        }

        #endregion
    }
}