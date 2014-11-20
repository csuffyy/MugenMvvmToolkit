using System.Windows;
using MugenMvvmToolkit.Infrastructure;

namespace $rootnamespace$
{
    public partial class App : Application
    {
        public App()
        {			
            new Bootstrapper<MainViewModel>(this, new IIocContainer());
        }
    }
}