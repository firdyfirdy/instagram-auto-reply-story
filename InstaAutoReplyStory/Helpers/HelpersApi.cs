using InstagramApiSharp.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace InstaAutoReplyStory.Helpers
{
  public static class HelpersApi
  {
    public static IInstaApi InstaApi { get; set; }
    
    public static void WriteLine(string value, ConsoleColor color = ConsoleColor.DarkGreen)
    {
      //
      // This method writes an entire line to the console with the string.
      //
      Console.ForegroundColor = color;
      Console.Write(value);
      Console.ResetColor();
      Console.WriteLine();
    }
  }
}
