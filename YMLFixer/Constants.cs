namespace YMLFixer
{
  /// <summary> Static string messages </summary>
  public abstract class Messages
  {
    public static readonly string InfoMessage = "{0}";
    public static readonly string ErrorMessage = "Error! {0}";
    public static readonly string WarningMessage = "Warning! {0}";
    public static readonly string YMLFound = "{0} yml files found.";
    public static readonly string ConfirmExitTitle = "Confirm Exit";
    public static readonly string ZeroYMLFound = "0 yml files found.";
    public static readonly string InputLocation = "Input location: {0}.";
    public static readonly string IDToBeRemoved = "ID to be removed: {0}.";
    public static readonly string EmptyFileList = "YML file list is empty.";
    public static readonly string InvalidEnteredID = "Entered ID is not valid.";
    public static readonly string YMLUpdated = "Total {0} out of {1} ymls updated.";
    public static readonly string LocationNotFound = "Location \"{0}\" doesn't exist.";
    public static readonly string FileDialogDescription = "Select YML source location...";
    public static readonly string RunAbortedNoYmlFile = "Couldn't find any yml file, run aborted.";
    public static readonly string InValidEnteredID = "Entered ID is not valid, can't apply filter.";
    public static readonly string WaitForIDSearch = "Performing ID based search operation in list, please wait...";
    public static readonly string ConfirmExit = "Processing underway, closing application may result corrupt yml file, exit anyway?";
    public static readonly string WaitForProcessing = "Processing YML list, aobve list will get updated with processed ymls, please wait...";
  }

  /// <summary> Static string data </summary>
  public abstract class Data
  {
    public static readonly string AppSettingFile = "YMLEditor.INI";
    public static readonly string SelectAllText = "Select All";
    public static readonly string UnSelectAllText = "Unselect All";
    public static readonly string SettingsHeader = "Settings";
    public static readonly string InputKey = "Input";
    public static readonly string LocationKey = "Location";
    public static readonly string SelectKey = "Selected";
    public static readonly string ProcessedColor = "SeaGreen";
    public static readonly string UnProcessedColor = "DarkGray";
    public static readonly string DefaultColor = "Black";
  }
}
