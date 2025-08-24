using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterCLS
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public ApplicationDataContainer Set=ApplicationData.Current.LocalSettings;
        public List<SearchResult> Cats = new();
        public ProfileParser parser;
        public MainWindow()
        {
            InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(GridTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
            InitializeFolder();
        }
        private void InitializeFolder()
        {
            string target=Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "database");
            string path = ValidationHelper.GetToken(Set, "Path");
            if (path!="0")
            {
                target = path;
            }
            parser=new ProfileParser(target);
        }
        private void search_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                Cats=parser.FindTeachersByName(sender.Text).OrderByDescending(x => x.Heat).Select(x => new SearchResult { Id=x.Id,Name=x.Name,College=x.College}).ToList();
                
                if (Cats.Count == 0)
                {
                    Cats.Add(new SearchResult { College="无",Id=0,Name="无匹配结果"});
                }
                sender.ItemsSource = Cats;
            }

        }

        private void search_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var item=args.SelectedItem as SearchResult;
            if (item != null)
            {
                var id=item.Id;
                if (id !=0)
                {
                    RootFrame.Navigate(typeof(Profile), id.ToString());
                }
            }
            
        }

        private void settings_Click(object sender, RoutedEventArgs e)
        {
            source.Text = ValidationHelper.GetToken(Set, "Path") == "0" ? "" : ValidationHelper.GetToken(Set, "Path");
            About.IsOpen = true;
        }
        private void close_Click(object sender, RoutedEventArgs e)
        {
            About.IsOpen = false;
        }

        private async void Git_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/Ginsenvey/BetterCLS/"));
        }

        private async void source_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            b.IsEnabled = false;
            FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();
            var window = (App.Current as App)._window;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                try
                {
                    var parser = new ProfileParser(folder.Path);
                    var profile = parser.FindTeacherById(1);
                    if (profile != null)
                    {
                        Set.Values["Path"] = folder.Path;
                        Flower.PlayAnimation("\uE930", "更新数据源成功");
                    }
                    else
                    {
                        Flower.PlayAnimation("\uEA39", "数据源为空");
                    }
                }
                catch
                {
                    Flower.PlayAnimation("\uEA39", "数据源不正确");
                }
            }
            b.IsEnabled = true;
        }
        private void rec_Click(object sender, RoutedEventArgs e)
        {
            Set.Values["Path"] = "0";
            Flower.PlayAnimation("\uE930", "重启应用生效");
        }
    }
}
