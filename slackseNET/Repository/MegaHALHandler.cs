using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace slackseNET
{
  public interface IMegaHALHandler
  {
    StreamWriter GetStandardInput();
    StreamReader GetStandardOutput();
    void Close();
  }


  public class MegaHALHandler : IMegaHALHandler
  {
    private static System.Diagnostics.Process MegaHALProcess;
    private static StreamReader StandardOutput;
    private static StreamWriter StandardInput;


    public MegaHALHandler()
    {
      try
      {
        MegaHALProcess = new Process();
        MegaHALProcess.StartInfo.UseShellExecute = false;
        MegaHALProcess.StartInfo.FileName = "./SVETSE/megahal";
        MegaHALProcess.StartInfo.CreateNoWindow = true;
        MegaHALProcess.StartInfo.RedirectStandardError = true;
        MegaHALProcess.StartInfo.RedirectStandardInput = true;
        MegaHALProcess.StartInfo.RedirectStandardOutput = true;
        MegaHALProcess.StartInfo.WorkingDirectory = "./SVETSE/";
        MegaHALProcess.StartInfo.StandardOutputEncoding = System.Text.Encoding.GetEncoding("iso-8859-1");

        MegaHALProcess.Start();

        StandardOutput = MegaHALProcess.StandardOutput;
        StandardInput = new StreamWriter(MegaHALProcess.StandardInput.BaseStream, System.Text.Encoding.GetEncoding("iso-8859-1"));
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
      }
    }

    public StreamReader GetStandardOutput()
    {
      return StandardOutput;
    }

    public StreamWriter GetStandardInput()
    {
      return StandardInput;
    }

    ~MegaHALHandler()
    {
      Close();
    }

    public void Close()
    {
      MegaHALProcess.Kill();
      MegaHALProcess.Dispose();
    }

  }


}


