using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Threading;
using System.ComponentModel;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace YMLFixer
{
  /// <summary> Removes specified id from all yml files of choosen location </summary>
  public partial class YMLEditor : Window, INotifyPropertyChanged
  {
    /// <summary> Constructor </summary>
    public YMLEditor()
    {
      fileList = new ObservableCollection<YMLFile>();
      this.DataContext = this;
      InitializeComponent();
      ThreadInvoker.Instance.InitDispatcher();
    }

    /// <summary> Property to access input outside </summary>
    public string Input
    {
      get { return _Input; }
      set
      {
        if (value != _Input)
        {
          _Input = value;
          RaisePropertyChanged("Input");
        }
      }
    }

    /// <summary> Property to access location outside </summary>
    public string Location
    {
      get { return _Location; }
      set
      {
        if (value != _Location)
        {
          _Location = value;
          RaisePropertyChanged("Location");
        }
      }
    }

    /// <summary> Property to access select/unselect all outside </summary>
    public bool IsSelected
    {
      get { return _IsSelected; }
      set
      {
        if (value != _IsSelected)
        {
          _IsSelected = value;
          RaisePropertyChanged("IsSelected");
        }
      }
    }

    /// <summary> Property to access apply/remove filter outside </summary>
    public bool FilterApplied
    {
      get { return _FilterApplied; }
      set
      {
        if (value != _FilterApplied)
        {
          _FilterApplied = value;
          RaisePropertyChanged("FilterApplied");
        }
      }
    }

    /// <summary> Property to access yml file list outside </summary>
    public ObservableCollection<YMLFile> YMLList
    {
      get { return fileList; }
      set
      {
        if (value != fileList)
        {
          fileList = value;
          RaisePropertyChanged("YMLList");
        }
      }
    }

    /// <summary> Property to access listbox outside </summary>
    public ListBox YMLListBox => lbYmls;

    /// <summary> Binding notification handler </summary>
    /// <param name="propertyName"> Name of property against which change triggered </param>
    protected void RaisePropertyChanged(string propertyName) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary> Displays specifed message on status bar based on type </summary>
    /// <param name="message"> message to be displayed </param>
    /// <param name="msgType"> type of message </param>
    /// <param name="title"> title of message, to be specified on exception </param>
    public void DisplayMessage(string message, MessageType msgType = MessageType.Info, string title = "")
    {
      switch (msgType)
      {
        case MessageType.Info:
          lbStatus.Content = string.Format(Messages.InfoMessage, message);
          break;
        case MessageType.Warning:
          lbStatus.Content = string.Format(Messages.WarningMessage, message);
          break;
        case MessageType.Error:
          lbStatus.Content = string.Format(Messages.ErrorMessage, message);
          break;
        case MessageType.Fatal:
          MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
          break;
      }
    }

    /// <summary> OnLoaded event, initializes app /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      try
      {
        fbd = new System.Windows.Forms.FolderBrowserDialog();
        ymlProcessor = new YMLProcessor(this);
        firstFileList = new List<YMLFile>();
        ThreadInvoker.Instance.InitDispatcher();
        settingINI = new INIProcessor(Data.AppSettingFile, this);
        string selected = (string)settingINI.GetValue(Data.SelectKey, Data.SettingsHeader);
        IsSelected = (selected == "0") ? false : true;
        Location = (string)settingINI.GetValue(Data.LocationKey, Data.SettingsHeader);
        if (_Location != null)
          LoadYMLFiles();

        Input = (string)settingINI.GetValue(Data.InputKey, Data.SettingsHeader);
        if (_Input != null && IsValidInput())
          txtInputID.ToolTip = string.Format(Messages.IDToBeRemoved, _Input);
      }
      catch(Exception ex)
      {
        DisplayMessage(ex.Message, MessageType.Fatal, "In: OnLoaded");
      }
    }

    /// <summary> OnClosing event, prepare for close </summary>
    private void OnClosing(object sender, CancelEventArgs e)
    {
      if (ymlThread != null && ymlThread.IsAlive)
      {
        if (MessageBox.Show(this, Messages.ConfirmExit, Messages.ConfirmExitTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) 
            == MessageBoxResult.No)
          e.Cancel = true;
        else
        {
          CloseIOFiles();
          ymlThread.Abort();
          SetProcessingMode(true);
          SaveSettings();
        }
      }
      else
        SaveSettings();
    }

    /// <summary> Saves application settings </summary>
    private void SaveSettings()
    {
      if (_Input == null && txtInputID.Text != null)
        Input = txtInputID.Text;

      if (IsValidInput())
        settingINI.SetValue(Data.InputKey, _Input, Data.SettingsHeader);
    }

    /// <summary> Event handler for set yml source, sets yml source location and loads files </summary>
    private void SetYMLSource(object sender, RoutedEventArgs e)
    {
      try
      {
        fbd.Description = Messages.FileDialogDescription;
        fbd.ShowNewFolderButton = false;
        if (Directory.Exists(_Location))
          fbd.SelectedPath = _Location;

        if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
          if (Location == fbd.SelectedPath)
          {
            LoadYMLFiles();
            FilterApplied = false;
            return;
          }

          Location = fbd.SelectedPath;
          settingINI.SetValue(Data.LocationKey, _Location, Data.SettingsHeader);
          LoadYMLFiles();
        }
      }
      catch(Exception ex)
      {
        DisplayMessage(ex.Message, MessageType.Fatal, "In: SetYMLSource");
      }
    }

    /// <summary> Loads yml file from choosen location </summary>
    private void LoadYMLFiles()
    {
      try
      {
        if(!Directory.Exists(_Location))
        {
          DisplayMessage(string.Format(Messages.LocationNotFound, _Location), MessageType.Error);
          return;
        }

        txtYMLSource.ToolTip = string.Format(Messages.InputLocation, _Location); ;
        DirectoryInfo inputDir = new DirectoryInfo(_Location);
        FileInfo[] files = inputDir.GetFiles("*.yml*", SearchOption.AllDirectories);
        if (files == null)
        {
          DisplayMessage(Messages.RunAbortedNoYmlFile, MessageType.Warning);
          lbYmls.ToolTip = Messages.ZeroYMLFound;
          return;
        }

        fileList.Clear();
        firstFileList.Clear();
        for (int i = 0; i < files.Length; i++)
        {
          string file = files[i].FullName;
          YMLFile ymlFile = new YMLFile { ID = (i + 1).ToString(), Name = file, Selected =_IsSelected };
          fileList.Add(ymlFile);
          firstFileList.Add(ymlFile);
        }

        lbYmls.ToolTip = string.Format(Messages.YMLFound, files.Length);
        DisplayMessage(lbYmls.ToolTip.ToString());
      }
      catch (Exception ex)
      {
        DisplayMessage(ex.Message, MessageType.Fatal, "In: LoadYMLFiles");
      }
    }

    /// <summary> Validates input id, input id should be valid guid without braces </summary>
    /// <param name="input"> guid without braces </param>
    /// <returns> true if valid, else false </returns>
    private bool IsValidInput(string input=null)
    {
      try
      {
        if (input == null)
          input = Input.Trim();

        string[] subIDs = input.Split('-');
        if (input.Length == 36 &&
            subIDs.Length == 5 &&
            subIDs[0].Length == 8 &&
            subIDs[1].Length == 4 &&
            subIDs[2].Length == 4 &&
            subIDs[3].Length == 4 &&
            subIDs[4].Length == 12)
          return true;

        return false;
      }
      catch (Exception ex)
      {
        DisplayMessage(ex.Message, MessageType.Fatal, "In: IsValidInput");
        return false;
      }
    }

    /// <summary> Attemps to copy guid from clipboard if found valid </summary>
    private void InputGotFocus(object sender, RoutedEventArgs e)
    {
      string input = Clipboard.GetText().Trim();
      if (!IsValidInput(input))
        return;

      else if (!string.IsNullOrEmpty(input))
        Input = input;
    }

    /// <summary> Closes input and output files </summary>
    public void CloseIOFiles()
    {
      try
      {
        if (ymlProcessor.InFile != null && ymlProcessor.InFile.BaseStream != null)
          ymlProcessor.InFile.Close();

        if (ymlProcessor.OutFile != null && ymlProcessor.OutFile.BaseStream != null)
          ymlProcessor.OutFile.Close();
      }
      catch {}
    }

    /// <summary> Sets application processing mode true or false </summary>
    /// <param name="iEnable"> true or false </param>
    public void SetProcessingMode(bool iEnable)
    {
      btnRemoveID.IsEnabled = iEnable;
      btnSetSource.IsEnabled = iEnable;
      txtInputID.IsEnabled = iEnable;
      cbFilter.IsEnabled = iEnable;
      cbSelect.IsEnabled = iEnable;
      lbYmls.IsEnabled = iEnable;
    }

    /// <summary> Event handler for select/unselect all checkbox. Selects or unselects all ymls </summary>
    private void OnSelect(object sender, RoutedEventArgs e)
    {
      cbSelect.Content = (_IsSelected) ? Data.UnSelectAllText : Data.SelectAllText;
      if (fileList.Count > 0)
        foreach (var file in fileList)
          file.Selected = _IsSelected;

      settingINI?.SetValue(Data.SelectKey, !_IsSelected ? "0" : "1", Data.SettingsHeader);
    }

    /// <summary> Event handler for apply/remove filter checkbox. Applies/removes ID filter on yml list </summary>
    private void OnApplyFilter(object sender, RoutedEventArgs e)
    {
      try
      {
        if (!IsValidInput())
        {
          DisplayMessage(Messages.InValidEnteredID, MessageType.Error);
          FilterApplied = false;
          return;
        }

        if (_FilterApplied == true)
        {
          SetProcessingMode(false);
          DisplayMessage(Messages.WaitForIDSearch);
          filterThread = new Thread(new ThreadStart(ApplyFilter))
          {
            Priority = ThreadPriority.Highest
          };
          filterThread.Start();
          filterThread.Join(100);
        }
        else
        {
          YMLList = new ObservableCollection<YMLFile>(firstFileList);
          lbYmls.ToolTip = string.Format(Messages.YMLFound, fileList.Count);
          DisplayMessage(lbYmls.ToolTip.ToString());
        }
      }
      catch (Exception ex)
      {
        DisplayMessage(ex.Message, MessageType.Fatal, "In: OnApplyFilter");
      }
    }

    /// <summary> Applies ID filter on yml list </summary>
    private void ApplyFilter()
    {
      try
      {
        List<YMLFile> notFoundList = new List<YMLFile>();
        for (int i = 0; i < fileList.Count; i++)
        {
          YMLFile file = fileList[i];
          string contents = File.ReadAllText(file.Name);
          if (!contents.Contains(_Input))
            notFoundList.Add(file);
        }

        if (notFoundList.Count > 0)
        {
          YMLList = new ObservableCollection<YMLFile>(fileList.Except(notFoundList));
          UpdateListIDs();
        }

        ThreadInvoker.Instance.RunByUiThread(() =>
        {
          lbYmls.ToolTip = string.Format(Messages.YMLFound, fileList.Count);
          DisplayMessage(lbYmls.ToolTip.ToString());
          SetProcessingMode(true);
        });
      }
      catch(Exception ex)
      {
        DisplayMessage(ex.Message, MessageType.Fatal, "In: ApplyFilter");
        SetProcessingMode(true);
      }
    }

    /// <summary> Event handler for removeID button, removes specified id from all ymls if found </summary>
    private void RemoveID(object sender, RoutedEventArgs e)
    {
      try
      {
        if(fileList.Count == 0)
        {
          DisplayMessage(Messages.EmptyFileList, MessageType.Error);
          return;
        }

        if (!IsValidInput())
        {
          DisplayMessage(Messages.InvalidEnteredID, MessageType.Error);
          return;
        }

        txtInputID.ToolTip = string.Format(Messages.IDToBeRemoved, _Input);
        settingINI.SetValue(Data.InputKey, _Input, Data.SettingsHeader);
        SetProcessingMode(false);
        DisplayMessage(Messages.WaitForProcessing);
        ymlThread = new Thread(new ThreadStart(ymlProcessor.ProcessYMLFiles))
        {
          Priority = ThreadPriority.Highest
        };
        ymlThread.Start();
        ymlThread.Join(100);
      }
      catch (Exception ex)
      {
        DisplayMessage(ex.Message, MessageType.Fatal, "In: RemoveID");
        DisplayMessage(ex.Message, MessageType.Error);
        SetProcessingMode(true);
      }
    }

    /// <summary> Updates list id on applying ID filter </summary>
    private void UpdateListIDs()
    {
      if (fileList.Count == 0)
        return;

      for (int i = 0; i < fileList.Count; i++)
        fileList[i].ID = (i + 1).ToString();
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public enum MessageType { Fatal, Error, Warning, Info };
    private System.Windows.Forms.FolderBrowserDialog fbd = null;
    private Thread ymlThread = null;
    private Thread filterThread = null;
    private string _Input = string.Empty;
    private string _Location = string.Empty;
    private bool _IsSelected = true;
    private bool _FilterApplied = false;
    private ObservableCollection<YMLFile> fileList = null;
    private List<YMLFile> firstFileList = null;
    private INIProcessor settingINI = null;
    private YMLProcessor ymlProcessor = null;
  }
}
