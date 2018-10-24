using System.ComponentModel;
using System.IO;

namespace YMLFixer
{
  /// <summary> YMLFile model to be used for listing </summary>
  public class YMLFile : INotifyPropertyChanged
  {
    /// <summary> Property to access ID </summary>
    public string ID
    {
      get { return _ID; }
      set
      {
        if (_ID != value)
        {
          _ID = value;
          RaisePropertyChanged("ID");
        }
      }
    }

    /// <summary> Property to access length of pattern </summary>
    public long Length => File.Exists(Name) ? new FileInfo(Name).Length : 0;

    /// <summary> Property to access existing pattern </summary>
    public string Name
    {
      get { return _Name; }
      set
      {
        if (_Name != value)
        {
          _Name = value;
          RaisePropertyChanged("Name");
        }
      }
    }

    /// <summary> Property to access selection value outside </summary>
    public bool Selected
    {
      get { return _Selected; }
      set
      {
        if (_Selected != value)
        {
          _Selected = value;
          RaisePropertyChanged("Selected");
        }
      }
    }

    /// <summary> Property to access item color outside </summary>
    public string Color
    {
      get { return _Color; }
      set
      {
        if (_Color != value)
        {
          _Color = value;
          RaisePropertyChanged("Color");
        }
      }
    }

    /// <summary> Binding notification handler </summary>
    /// <param name="propertyName"> Name of property against which change triggered </param>
    protected void RaisePropertyChanged(string propertyName) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler PropertyChanged;
    private string _ID = string.Empty;
    private string _Name = string.Empty;
    private bool _Selected = true;
    private string _Color = Data.DefaultColor;
  }
}
