using System.ComponentModel;
using System.Windows.Media;

namespace Vadász_Mars_Dénes
{
    public class MapCell : INotifyPropertyChanged
    {
        private ImageSource _imageSource;
        private bool _hasVisited;

        public ImageSource ImageSource
        {
            get => _imageSource;
            set { _imageSource = value; OnPropertyChanged(nameof(ImageSource)); }
        }

        // Új tulajdonság a nyomvonal jelzéséhez
        public bool HasVisited
        {
            get => _hasVisited;
            set { _hasVisited = value; OnPropertyChanged(nameof(HasVisited)); }
        }

        public string Coordinates { get; set; }
        public string Type { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}