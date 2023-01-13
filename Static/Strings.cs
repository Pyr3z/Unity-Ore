/*! @file   Static/Strings.cs
 *  @author levianperez\@gmail.com
 *  @author levi\@leviperez.dev
 *  @date   2020-06-06
 *
 *  @brief
 *    Utilities for C# string manipulation & querying.
**/

using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using StringBuilder = System.Text.StringBuilder;
using Encoding      = System.Text.Encoding;

using Convert       = System.Convert;
using IFormatter    = System.IFormatProvider;
using IConvertible  = System.IConvertible;

using MethodImplAttribute = System.Runtime.CompilerServices.MethodImplAttribute;
using MethodImplOptions   = System.Runtime.CompilerServices.MethodImplOptions;


namespace Ore
{
  /// <summary>
  /// Utilities for C# string manipulation & querying.
  /// </summary>
  [PublicAPI]
  public static class Strings
  {
    public static Encoding DefaultEncoding { get; set; } = Encoding.Unicode;

    public static IFormatter InvariantFormatter { get; set; } = System.Globalization.CultureInfo.InvariantCulture;


    public static readonly char[] WHITESPACES = { ' ', '\t', '\n', '\r', '\v' };



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this string str)
    {
      return str is null || str.Length == 0;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToInvariant([NotNull] this IConvertible self)
    {
      return self.ToString(InvariantFormatter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToInvariantLower([NotNull] this IConvertible self)
    {
      return self.ToString(InvariantFormatter).ToLowerInvariant();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToInvariantUpper([NotNull] this IConvertible self)
    {
      return self.ToString(InvariantFormatter).ToUpperInvariant();
    }


    public static byte[] ToBytes(this string str, Encoding encoding = null)
    {
      if (IsEmpty(str))
        return System.Array.Empty<byte>();

      return (encoding ?? DefaultEncoding).GetBytes(str);
    }


    public static string FromBytes([CanBeNull] byte[] bytes, [CanBeNull] Encoding encoding = null)
    {
      if (bytes is null || bytes.Length == 0)
        return string.Empty;

      return (encoding ?? DefaultEncoding).GetString(bytes);
        // can throw ArgumentException if the bytes violate the encoding provided
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase64(this string str, Encoding encoding = null)
    {
      return Convert.ToBase64String(str.ToBytes(encoding));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ParseBase64(this string str, Encoding encoding = null)
    {
      return FromBytes(Convert.FromBase64String(str), encoding);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string MakeGUID()
    {
      #if UNITY_EDITOR
      return UnityEditor.GUID.Generate().ToString();
      #else
      return System.Guid.NewGuid().ToString("N");
      #endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string MakeISOTimezone(System.TimeSpan offset)
    {
      return $"{(offset.Ticks < 0 ? '-' : '+')}{offset:hhmm}";
    }

    public static string MakeISO6391(SystemLanguage lang)
    {
      switch (lang)
      {
        case SystemLanguage.Afrikaans:
          return "AF";
        case SystemLanguage.Arabic:
          return "AR";
        case SystemLanguage.Basque:
          return "EU";
        case SystemLanguage.Belarusian:
          return "BY";
        case SystemLanguage.Bulgarian:
          return "BG";
        case SystemLanguage.Catalan:
          return "CA";
        case SystemLanguage.Chinese:
          return "ZH";
        case SystemLanguage.Czech:
          return "CS";
        case SystemLanguage.Danish:
          return "DA";
        case SystemLanguage.Dutch:
          return "NL";
        case SystemLanguage.English:
          return "EN";
        case SystemLanguage.Estonian:
          return "ET";
        case SystemLanguage.Faroese:
          return "FO";
        case SystemLanguage.Finnish:
          return "FI"; // WARNING: has a collision with tagalog ("TL")
        case SystemLanguage.French:
          return "FR";
        case SystemLanguage.German:
          return "DE";
        case SystemLanguage.Greek:
          return "EL";
        case SystemLanguage.Hebrew:
          return "HE";
        case SystemLanguage.Hungarian:
          return "HU";
        case SystemLanguage.Icelandic:
          return "IS";
        case SystemLanguage.Indonesian:
          return "ID";
        case SystemLanguage.Italian:
          return "IT";
        case SystemLanguage.Japanese:
          return "JA";
        case SystemLanguage.Korean:
          return "KO";
        case SystemLanguage.Latvian:
          return "LV";
        case SystemLanguage.Lithuanian:
          return "LT";
        case SystemLanguage.Norwegian:
          return "NB";
        case SystemLanguage.Polish:
          return "PL";
        case SystemLanguage.Portuguese:
          return "PT"; // TODO differentiate between PT-BR?
        case SystemLanguage.Romanian:
          return "RO";
        case SystemLanguage.Russian:
          return "RU";
        case SystemLanguage.SerboCroatian:
          return "SH";
        case SystemLanguage.Slovak:
          return "SK";
        case SystemLanguage.Slovenian:
          return "SL";
        case SystemLanguage.Spanish:
          return "ES";
        case SystemLanguage.Swedish:
          return "SV";
        case SystemLanguage.Thai:
          return "TH";
        case SystemLanguage.Turkish:
          return "TR";
        case SystemLanguage.Ukrainian:
          return "UK";
        case SystemLanguage.Vietnamese:
          return "VI";
        case SystemLanguage.ChineseSimplified:
          return "ZH-CN";
        case SystemLanguage.ChineseTraditional:
          return "ZH-TW";

        case SystemLanguage.Unknown:
        default:
          // caller can decide what a good default value is if empty.
          return string.Empty;
      }
    }


    public static string ExpandCamelCase([CanBeNull] this string str)
    {
      if (str is null || str.Length <= 1)
        return str;

      int i = 0, ilen = str.Length;

      if (str[1] == '_')
      {
        // handles the forms "m_Variable", "s_StaticStuff" ...
        if (str.Length == 2)
          return str;

        i = 2;
      }
      else if (char.IsLower(str[0]) && char.IsUpper(str[1]))
      {
        // handles "mVariable", "aConstant"...
        i = 1;
      }

      char c = str[i];
      bool in_word = false;
      var bob = new StringBuilder(ilen + 8);

      if (char.IsLower(c)) // adjusts for lower camel case
      {
        in_word = true;
        bob.Append(char.ToUpper(c));
        ++i;
      }

      while (i < ilen)
      {
        c = str[i];

        if (char.IsLower(c) || char.IsDigit(c))
        {
          in_word = true;
        }
        else if (in_word && (char.IsUpper(c) || c == '_'))
        {
          bob.Append(' ');
          in_word = false;
        }

        if (char.IsLetterOrDigit(c))
        {
          bob.Append(c);
        }

        ++i;
      }

      return bob.ToString();
    }


    public static int CountAny(string str, params char[] chars)
    {
      var charset = new HashSet<char>(chars);

      int count = 0;

      foreach (char c in str)
      {
        if (charset.Contains(c))
          ++count;
      }

      return count;
    }

    public static int CountDigits(string str)
    {
      int count = 0;

      foreach (char c in str)
      {
        if (char.IsDigit(c))
          ++count;
      }

      return count;
    }

    public static int CountContiguousDigits(string str)
    {
      int count = 0, max = 0;

      foreach (char c in str)
      {
        if (char.IsDigit(c))
        {
          ++count;
        }
        else if (c != '.' && count > 0)
        {
          if (max < count)
            max = count;
          count = 0;
        }
      }

      return max < count ? count : max;
    }


    public static string RemoveHypertextTags([CanBeNull] string text)
    {
      // algorithm formerly called "PyroDK.RichText.RemoveSoberly(text)"

      if (text is null || text.Length < 3)
        return text;

      var bob = new StringBuilder(text.Length / 2);

      int i = 0, size = text.Length;
      (int start, int end) lhs = (0, 0),
                           rhs = (0, size - 1);

      int found = 0; // 1 = found open, 2 = found pair
      while (i < size)
      {
        if (text[i] == '<')
        {
          if (found == 0 && text[i + 1] != '/')
          {
            int lhs_end = i - 1;

            while (++i < size)
            {
              if (text[i] == '>')
              {
                found = 1;
                lhs.end = lhs_end;
                rhs.start = i + 1;
                break;
              }
            }
          }
          else if (found == 1 && text[i + 1] == '/')
          {
            int rhs_end = i - 1;

            while (++i < size)
            {
              if (text[i] == '>')
              {
                found = 2;
                rhs.end = rhs_end;
                break;
              }
            }
          }
        }

        ++i;

        if (found == 2)
        {
          lhs.end -= lhs.start - 1;
          rhs.end -= rhs.start - 1;

          if (lhs.end > 0)
            bob.Append(text, lhs.start, lhs.end);
          if (rhs.end > 0)
            bob.Append(text, rhs.start, rhs.end);

          lhs.start = lhs.end = rhs.start = i;
          rhs.end = size - 1;
          found = 0;
        }
      } // end outer while loop

      if (found == 0 && (rhs.end -= rhs.start - 1) > 0)
      {
        bob.Append(text, rhs.start, rhs.end);
      }

      return bob.ToString();
    }

  } // end static class Strings

}
