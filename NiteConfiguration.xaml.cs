using System;
using System.Windows;

namespace NITEProvider
{
    /// <summary>
    /// Interaction logic for NiteConfiguration.xaml
    /// </summary>
    public partial class NiteConfiguration 
    {
        public NiteConfiguration()
        {
            DataContext = this.DataContext;
            InitializeComponent();
        }
    }
}
