using Andy.WPF.SimpleMVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace ImgQualityConverter
{
    public class MainWindowViewModel : BaseViewModel
    {

        #region Instance

        private static MainWindowViewModel _Instance;

        public static MainWindowViewModel Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new MainWindowViewModel();
                }
                return _Instance;
            }
        }

        #endregion

        private bool _IsConvertForSingleFile;
        public bool IsConvertForSingleFile
        {
            get
            {
                return _IsConvertForSingleFile;
            }
            set
            {
                _IsConvertForSingleFile = value;
                NotifyPropertyChanged("IsConvertForSingleFile");
                NotifyPropertyChanged("IsConvertForMultiFile");
            }
        }
        public bool IsConvertForMultiFile
        {
            get
            {
                return !_IsConvertForSingleFile;
            }
            set
            {
                _IsConvertForSingleFile = !value;
                NotifyPropertyChanged("IsConvertForSingleFile");
                NotifyPropertyChanged("IsConvertForMultiFile");
            }
        }

        private string _Path;
        public string Path
        {
            get
            {
                return _Path;
            }
            set
            {
                _Path = value;
                NotifyPropertyChanged("Path");
            }
        }

        private int _Quality;
        public int Quality
        {
            get
            {
                return _Quality;
            }
            set
            {
                _Quality = Math.Max(0, Math.Min(value, 100));
                NotifyPropertyChanged("Quality");
            }
        }

        MainWindowViewModel()
        {
            Revert();
            Path = ConfigurationManager.AppSettings["Path"];
        }

        public void Revert()
        {
            IsConvertForSingleFile = Boolean.Parse(ConfigurationManager.AppSettings["IsConvertForSingleFile"]);
            Path = ConfigurationManager.AppSettings["DefaultPath"];
            Quality = Int32.Parse(ConfigurationManager.AppSettings["Quality"]);
        }

        public void Save()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["Path"].Value = Path;
            config.Save();
        }
    }
}
