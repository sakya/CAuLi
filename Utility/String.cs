using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Utility
{
  public static class String
  {
    /// <summary>
    /// Encrypt a string using SHA1
    /// </summary>
    /// <param name="input">The string to encrypt</param>
    /// <returns>The encrypted string</returns>
    public static string GetSHA1Hash(string input)
    {
      if (string.IsNullOrEmpty(input))
        return string.Empty;

      using (var sha1 = SHA1.Create()) {
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(hash.Length * 2);

        foreach (byte b in hash)
          sb.Append(b.ToString("x2"));
        return sb.ToString();
      }
    } // GetSHA1Hash

    /// <summary>
    /// Encrypt a string using MD5
    /// </summary>
    /// <param name="input">The string to encrypt</param>
    /// <returns>The encrypted string</returns>
    public static string GetMD5Hash(string input)
    {
      if (string.IsNullOrEmpty(input))
        return string.Empty;
      MD5 md5 = MD5.Create();
      byte[] inputBytes = Encoding.ASCII.GetBytes(input);
      byte[] hash = md5.ComputeHash(inputBytes);

      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < hash.Length; i++) {
        sb.Append(hash[i].ToString("X2"));
      }
      return sb.ToString();
    } // GetMD5Hash

    public static List<string> SplitStringInLines(string str, int width)
    {
      List<char> wordSeparators = new List<char>() { ' ', '.', ':', ',', ';', '!', '?' };
      List<string> lines = new List<string>();
      // Split in lines if needed:
      if (str.Length > width || str.Contains("\n")) {
        StringBuilder line = new StringBuilder();
        StringBuilder word = new StringBuilder();
        for (int i = 0; i < str.Length; i++) {
          char c = str[i];
          if (c == 10) {
            // Newline \n
            if (word.Length > 0) {
              line.Append(word.ToString());
              word.Clear();
            }
            lines.Add(line.ToString());
            line.Clear();
            line.Append(word.ToString());
          } else if (c == 13) {
            // Ignore \r
          } else if (wordSeparators.Contains(c)) {
            if (line.Length + word.Length + 1 < width) {
              line.Append(word.ToString());
              line.Append(c);
            } else {
              lines.Add(line.ToString());
              line.Clear();
              line.Append(word.ToString());
              if (line.Length > 0 || c != ' ')
                line.Append(c);
            }
            word.Clear();
          } else {
            word.Append(c);
            if (line.Length + word.Length + 1 >= width) {
              lines.Add(line.ToString());
              line.Clear();
            }
          }
        }

        if (word.Length > 0) {
          line.Append(word.ToString());
          word.Clear();
        }
        if (line.Length > 0)
          lines.Add(line.ToString());
      } else
        lines.Add(str);

      return lines;
    } // SplitStringInLines
  }
}
