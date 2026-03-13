using System.ComponentModel;
using System.Windows.Media;

namespace Vadász_Mars_Dénes
{
    public class MapCell : INotifyPropertyChanged
    {
        private ImageSource _imageSource;
        private bool _hasVisited;
        private double _rotationAngle; // Új mező a forgatáshoz

        public ImageSource ImageSource
        {
            get => _imageSource;
            set { _imageSource = value; OnPropertyChanged(nameof(ImageSource)); }
        }

        public bool HasVisited
        {
            get => _hasVisited;
            set { _hasVisited = value; OnPropertyChanged(nameof(HasVisited)); }
        }

        // Új tulajdonság az elforgatás szögének tárolásához
        public double RotationAngle
        {
            get => _rotationAngle;
            set { _rotationAngle = value; OnPropertyChanged(nameof(RotationAngle)); }
        }

        public string Coordinates { get; set; }
        public string Type { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}