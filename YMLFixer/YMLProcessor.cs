using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace YMLFixer
{
  /// <summary> YMLProcessor to process yml files </summary>
  public class YMLProcessor
  {
    /// <summary> Constructor </summary>
    /// <param name="handler"> Handler window </param>
    public YMLProcessor(YMLEditor handler) => ymlEditor = handler;

    /// <summary> Property to access input file stream </summary>
    public StreamReader InFile { get; private set; } = null;

    /// <summary> Property to access output file stream </summary>
    public StreamWriter OutFile { get; private set; } = null;

    /// <summary> Processes all yml files and removes ID if found </summary>
    public void ProcessYMLFiles()
    {
      try
      {
        int filesWritten = 0;
        for (int i = 0; i < ymlEditor.YMLList.Count; i++)
        {
          YMLFile ymlFile = ymlEditor.YMLList[i];
          List<string> lines = new List<string>();
          InFile = new StreamReader(ymlFile.Name, Encoding.Default);
          if (InFile == null || !ymlFile.Selected)
            continue;

          Encoding inFileEncoding = InFile.CurrentEncoding;
          string strLine = string.Empty;
          while (strLine != null)
          {
            strLine = InFile.ReadLine();
            if (strLine != null)
              lines.Add(strLine);
          }

          InFile.Close();
          if (WriteFile(ymlFile.Name, ref lines, inFileEncoding))
          {
            ymlFile.Color = Data.ProcessedColor;
            filesWritten++;
          }
          else
            ymlFile.Color = Data.UnProcessedColor;
        }

        ThreadInvoker.Instance.RunByUiThread(() =>
        {
          ymlEditor.YMLListBox.ToolTip = string.Format(Messages.YMLFound, ymlEditor.YMLList.Count);
          ymlEditor.DisplayMessage(string.Format(Messages.YMLUpdated, filesWritten, ymlEditor.YMLList.Count));
          ymlEditor.SetProcessingMode(true);
        });
      }
      catch (Exception ex)
      {
        ymlEditor.CloseIOFiles();
        ThreadInvoker.Instance.RunByUiThread(() =>
        {
          ymlEditor.DisplayMessage(ex.Message, YMLEditor.MessageType.Fatal, "In: ProcessYMLFiles");
          ymlEditor.SetProcessingMode(true);
        });
      }
    }

    /// <summary> Check if skip headers found </summary>
    /// <param name="line"> data where skip header to be checked </param>
    /// <returns> true if skip header found else false </returns>
    private bool SkipHeaderFound(string line)
    {
      if (line.Length < 5)
        return false;

      foreach (var header in SkipHeaders)
        if (line.Contains(header))
          return true;

      return false;
    }

    /// <summary> Writes yml files after removing specified id </summary>
    /// <param name="file"> fully qualified file name </param>
    /// <param name="lines"> lines of file </param>
    /// <param name="inFileEncoding"> encoding of input file </param>
    /// <returns> true if modified, else false </returns>
    private bool WriteFile(string file, ref List<string> lines, Encoding inFileEncoding)
    {
      try
      {
        string lineToSearch = string.Format("- ID: \"{0}\"", ymlEditor.Input.ToLower());
        string foundLine = lines.FirstOrDefault(x => (x.Contains(lineToSearch) == true));
        if (foundLine == null)
          return false;

        File.Delete(file);
        OutFile = new StreamWriter(file, false, inFileEncoding);
        if (OutFile == null)
          return false;

        bool ignore = false;
        foreach (var line in lines)
        {
          if (line.Contains(lineToSearch))
          {
            ignore = true;
            continue;
          }

          if (ignore && SkipHeaderFound(line))
            ignore = false;

          if (!ignore)
            OutFile.WriteLine(line);
        }

        OutFile.Close();
        return true;
      }
      catch (Exception ex)
      {
        ymlEditor.CloseIOFiles();
        ymlEditor.DisplayMessage(ex.Message, YMLEditor.MessageType.Fatal, "In: WriteFile");
        return false;
      }
    }

    private YMLEditor ymlEditor = null;
    private readonly string[] SkipHeaders = { "- ID:", "Languages:", "Versions:" };
  }
}
