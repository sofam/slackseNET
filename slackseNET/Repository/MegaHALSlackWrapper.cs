/*
  Wraps the MegaHAL process and removes slack specific stuff from the input and output
 */


using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace slackseNET
{
  public class MegaHALSlackWrapper
  {
    public MegaHALSlackWrapper(IMegaHALHandler MegaHAL)
    {
      StandardOutput = new SlackStreamReader(MegaHAL.GetStandardOutput().BaseStream, System.Text.Encoding.GetEncoding("iso-8859-1"));
      StandardInput = new SlackStreamWriter(MegaHAL.GetStandardInput().BaseStream, System.Text.Encoding.GetEncoding("iso-8859-1"));
    }

    private static SlackStreamReader StandardOutput;
    private static SlackStreamWriter StandardInput;

    public SlackStreamWriter GetStandardInput()
    {
      return StandardInput;
    }

    public SlackStreamReader GetStandardOutput()
    {
      return StandardOutput;
    }

    public class SlackStreamWriter : StreamWriter
    {
      public SlackStreamWriter(Stream stream) : base(stream)
      {

      }

      public SlackStreamWriter(Stream stream, System.Text.Encoding encoding) : base(stream, encoding)
      {

      }

      public override void WriteLine(string value)
      {
        var NickStrippedResponse = Regex.Replace(value, "<.*?>", "").TrimStart();
        var NewlineStrippedResponse = Regex.Replace(NickStrippedResponse, "\n", " ");
        base.WriteLine(NewlineStrippedResponse);
      }
    }

    public class SlackStreamReader : StreamReader
    {
      public SlackStreamReader(Stream stream) : base(stream)
      {

      }

      public SlackStreamReader(Stream stream, System.Text.Encoding encoding) : base(stream, encoding)
      {

      }

      public override async Task<string> ReadLineAsync()
      {
        var UnformattedResponse = await base.ReadLineAsync();
        return UnformattedResponse.TrimStart(new char[] { '>', '-', ' ' });
      }

    }

  }



}