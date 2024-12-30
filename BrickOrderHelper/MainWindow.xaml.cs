using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Windows.Automation;
using System.DirectoryServices.ActiveDirectory;
using System.Globalization;
using System.Net.Http;
using System.Security.Policy;
using HtmlAgilityPack;
using System.Net;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Threading;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth;
using System.Collections;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Data;
using System.Windows.Markup;
using FastMember;

namespace BrickOrderHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? fileContents = null;
        private Dictionary<string, Dictionary<string, string>> colourDictionary = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, Dictionary<string, string>> partDictionary = new Dictionary<string, Dictionary<string, string>>();
        private ObservableCollection<Element> elements = new ObservableCollection<Element>();
        private List<Task> tasks = new List<Task>();
        private string consumerKey = "A3C417FD836D4A87A9C372F6ACD1D1CD";
        private string consumerSecret = "794DCC189C8A4E3B9C1443013F8C0FB2";
        private string tokenKey = "C9482C39FDFD4B74B2A2E820F18F1A0F";
        private string tokenSecret = "AFF4E635500E489C87244438BF49A71B";
        private List<string> userAgents = new List<string>() { "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.361675787110",
"Mozilla/5.0 (Macintosh; Intel Mac OS X 11_3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.5412.99 Safari/537.36",
"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_10) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.5361.172 Safari/537.36",
"Mozilla/5.0 (X11; Linux i686) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.5388.177 Safari/537.36",
"Mozilla/5.0 (Macintosh; Intel Mac OS X 11_14) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.5397.215 Safari/537.36"};
        public MainWindow()
        {
            InitializeComponent();
            GetLegoToBricklinkColours();
            //GetRebrickablePartInfo();
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            _ElementGrid.ItemsSource = elements;
           
            elements.CollectionChanged += Elements_CollectionChanged;
        }

        private int GetRunningTaskCount()
        {
            int runningTaskCount = 0;
            foreach (Task task in tasks)
            {
                if (task.Status == TaskStatus.Running)
                {
                    runningTaskCount++;
                }
            }
            return runningTaskCount;
        }

        private void CleanUpTasks()
        {
            List<Task> tasksToRemove = new List<Task>();
            foreach (Task task in tasks)
            {
                if (task.IsCompleted)
                {
                    tasksToRemove.Add(task);
                }
            }
            foreach (Task task in tasksToRemove)
            {
                tasks.Remove(task);
            }
        }

        private async void Elements_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems.Count > 0)
            {
                RestClient client = GetBricklinkRestClient();
                foreach (var newItem in e.NewItems)
                {
                    //int runningTaskCount = GetRunningTaskCount();
                    //while (runningTaskCount > 9)
                    //{
                    //    await Task.Delay(100);
                    //    runningTaskCount = GetRunningTaskCount();
                    //}
                    Element element = (Element)newItem;

                    tasks.Add( Task.Run(() => UpdateElementInfo(element, client)));
                    //await Task.Delay(10);
                    Thread.Sleep(10);
                    //CleanUpTasks();
                }
            }
        }

        private async Task UpdateElementInfo(Element element, RestClient client)
        {
            await UpdateElementName(client, element);
            Task.Run(() => UpdateElementBrickLinkPrice(client, element));
            Task.Run(() => UpdateElementPabPrice(element));
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private async void GetRebrickablePartInfo()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://rebrickable.com/api/v3/lego/parts/");

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("key", "8fcc57c0993d175cdf705c0db166335f");

            // List data response.
            string response = await client.GetStringAsync("https://rebrickable.com/api/v3/lego/parts/");  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            if (!string.IsNullOrEmpty(response))
            {
                Dictionary<string, object> dataObjects = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                List<Dictionary<string, object>> results = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(dataObjects["results"].ToString());
                while (dataObjects["next"] != null)
                {
                    response = await client.GetStringAsync(dataObjects["next"].ToString());
                    if (!string.IsNullOrEmpty(response))
                    {
                        dataObjects = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                        results.AddRange(JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(dataObjects["results"].ToString()));

                    }
                }
            }
        }

        private void GetLegoToBricklinkColours()
        {
            WebClient webClient = new WebClient();
            webClient.Proxy = WebRequest.DefaultWebProxy;
            string page = webClient.DownloadString("http://www.peeron.com/inv/colors");

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(page);
            List<List<string>> tableHeaders = doc.DocumentNode.SelectSingleNode("//body/table")
                        .Descendants("tr")
                        .Where(tr => tr.Elements("th").Count() > 1)
                        .Select(tr => tr.Elements("th").Select(th => th.InnerText.Trim()).ToList())
                        .ToList();
            List<List<string>> table = doc.DocumentNode.SelectSingleNode("//body/table")
                        .Descendants("tr")
                        .Where(tr => tr.Elements("td").Count() > 1)
                        .Select(tr => tr.Elements("td").Select(td => td.InnerText.Trim()).ToList())
                        .ToList();
            List<Dictionary<string, string>> colourList = new List<Dictionary<string, string>>();
            foreach (List<string> row in table)
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                for (int i = 0; i < row.Count; i++)
                {
                    dictionary[tableHeaders[0][i]] = row[i];
                }
                colourList.Add(dictionary);
            }
            foreach (Dictionary<string, string> colour in colourList)
            {
                colourDictionary[colour["BLID"]] = colour;
            }

        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                _FileNameTextBox.Text = openFileDialog.FileName;
            }
        }

        private async void FileName_TextChanged(object sender, TextChangedEventArgs args)
        {
            if (File.Exists(_FileNameTextBox.Text))
            {
                fileContents = File.ReadAllText(_FileNameTextBox.Text);
                if (Regex.IsMatch(_FileNameTextBox.Text, ".*\\.xml"))
                {
                    XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
                    xmlDoc.Load(_FileNameTextBox.Text);
                    if (xmlDoc.DocumentElement == null) { return; }
                    //RestClient client = GetBricklinkRestClient();
                    foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                    {
                        Element element = new Element();
                        //element.colourDictionary = colourDictionary;
                        foreach (XmlNode childNode in node.ChildNodes)
                        {
                            //< ITEMTYPE > P </ ITEMTYPE >
                            //< ITEMID > 2445 </ ITEMID >
                            //< COLOR > 11 </ COLOR >
                            //< MAXPRICE > -1.0000 </ MAXPRICE >
                            //< MINQTY > 1 </ MINQTY >
                            //< CONDITION > X </ CONDITION >
                            //< NOTIFY > N </ NOTIFY >
                            //var format = new NumberFormatInfo();
                            //format.NegativeSign = "-";
                            //format.NumberDecimalSeparator = ".";
                            switch (childNode.Name)
                            {
                                case "ITEMTYPE":
                                    {
                                        element.ItemType = childNode.InnerText;
                                        break;
                                    }
                                case "ITEMID":
                                    {
                                        element.ItemID = childNode.InnerText;
                                        //var response = CallUrl("https://www.lego.com/en-gb/pick-and-build/pick-a-brick?ef_id=CjwKCAiAuNGuBhAkEiwAGId4amCZbEhudyoh_aUtbSKqLSiO_n56a6LAz5R7FGxgNzEATRvVB9aI8RoCpNYQAvD_BwE%3AG%3As&s_kwcid=AL%21933%213%21675988700144%21b%21%21g%21%21lego+pick+a+brick%2120617975117%21153795026546&cmp=KAC-INI-GOOGEU-GO-GB_GL-EN-RE-PS-BUY-CREATE-LEGO_GENERAL-SHOP-ThInt-MM-ALL-CIDNA00000-PICK_A_BRICK&gad_source=1&designNumber=3024&color=140").Result;

                                        break;
                                    }
                                case "COLOR":
                                    {
                                        element.BrickLinkColour = int.Parse(childNode.InnerText);
                                        element.Colour = colourDictionary[element.BrickLinkColour.ToString()]["BLName"];
                                        break;
                                    }
                                case "MAXPRICE":
                                    {
                                        element.MaxPrice = double.Parse(childNode.InnerText);
                                        break;
                                    }
                                case "MINQTY":
                                    {
                                        element.MinQty = int.Parse(childNode.InnerText);
                                        break;
                                    }
                                case "CONDITION":
                                    {
                                        element.Condition = childNode.InnerText;
                                        break;
                                    }
                                case "NOTIFY":
                                    {
                                        element.Notify = childNode.InnerText;
                                        break;
                                    }
                            }
                        }
                        elements.Add(element);
                        //var task1 = Task.Run(() => UpdateElementPabPrice(element));
                        //await UpdateElementName(client, element);
                        //Thread.Sleep(10);
                        //await UpdateElementBrickLinkPrice(client, element);
                        //Thread.Sleep(10);

                    }
                }
                else if (Regex.IsMatch(_FileNameTextBox.Text, ".*\\.csv"))
                {

                }
            }
        }
        private RestClient GetBricklinkRestClient()
        {
            RestClientOptions options = new RestClientOptions($"https://api.bricklink.com/api/store/v1/");

            options.Authenticator = OAuth1Authenticator.ForProtectedResource(consumerKey, consumerSecret, tokenKey, tokenSecret);

            RestClient client = new RestClient(options);
            return client;
        }

        private async Task<Dictionary<string, object>> QueryBricklinkRestApi(RestClient client, string query)
        {
            RestRequest request = new RestRequest(query);
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    RestResponse response = await client.GetAsync(request);

                    Dictionary<string, object> responseObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);

                    Dictionary<string, object> partInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseObject["data"].ToString());

                    return partInfo;
                }
                catch (HttpRequestException)
                {
                }
            }
            return new Dictionary<string, object>();
        }

        private async Task UpdateElementBrickLinkPrice(RestClient client, Element element)
        {
            string type = "part";

            Dictionary<string, object> partInfo = await QueryBricklinkRestApi(client, $"items/{type}/{element.ItemID}/price?color_id={element.BrickLinkColour}");
            if (partInfo != null && partInfo.ContainsKey("avg_price"))
            {
                element.BrickLinkPrice = double.Parse(partInfo["avg_price"].ToString());
            }
            this.Dispatcher.Invoke(() =>
            {
                _ElementGrid.Items.Refresh();
            });
            //this.Dispatcher.BeginInvoke(() =>
            //{
            //    UpdateGridColours(element);
            //});
        }

        private async Task UpdateElementName(RestClient client, Element element)
        {
            string type = "part";

            Dictionary<string, object> partInfo = await QueryBricklinkRestApi(client, $"items/{type}/{element.ItemID}");

            if (partInfo != null && partInfo.ContainsKey("name"))
            {
                element.Name = partInfo["name"].ToString();
            }
            if (partInfo != null && partInfo.ContainsKey("alternate_no"))
            {
                element.AlternateIds = partInfo["alternate_no"].ToString().Split(",");
            }
            else
            {
                element.AlternateIds = new string[0];
            }
            this.Dispatcher.Invoke(() =>
            {
                _ElementGrid.Items.Refresh();
            });
        }

        private async Task UpdateElementPabPrice(Element element)
        {
            WebClient webClient = new WebClient();
            webClient.Proxy = WebRequest.DefaultWebProxy;
            string[] ids = element.AlternateIds;
            Array.Resize(ref ids, ids.Length + 1);
            ids[^1] = element.ItemID;

            foreach (string itemId in ids.Reverse())
            {
                string legoUrl = $"https://www.lego.com/en-gb/pick-and-build/pick-a-brick?ef_id=CjwKCAiAuNGuBhAkEiwAGId4amCZbEhudyoh_aUtbSKqLSiO_n56a6LAz5R7FGxgNzEATRvVB9aI8RoCpNYQAvD_BwE%3AG%3As&s_kwcid=AL%21933%213%21675988700144%21b%21%21g%21%21lego+pick+a+brick%2120617975117%21153795026546&cmp=KAC-INI-GOOGEU-GO-GB_GL-EN-RE-PS-BUY-CREATE-LEGO_GENERAL-SHOP-ThInt-MM-ALL-CIDNA00000-PICK_A_BRICK&gad_source=1&designNumber={itemId}&color={colourDictionary[element.BrickLinkColour.ToString()]["LEGOID"]}";

                HtmlDocument legoDoc = new HtmlAgilityPack.HtmlDocument();
                legoDoc.LoadHtml(webClient.DownloadString(legoUrl));

                HtmlNode legoHtmlNode = legoDoc.DocumentNode.SelectSingleNode("//div[contains(@data-test, \"pab-item-price\")]");
                if (legoHtmlNode == null) { continue; }

                string price = Regex.Replace(legoHtmlNode.InnerText, "£", "").ToString();
                element.LegoPrice = double.Parse(price);
                
                this.Dispatcher.Invoke(() =>
                {
                    _ElementGrid.Items.Refresh();
                });
                break;
            }
            //this.Dispatcher.BeginInvoke(() =>
            //{
            //    UpdateGridColours(element);
            //});
        }

        private void ElementGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DataGridRow row = (DataGridRow)e.Row;
            if (row.Background != Brushes.LightGreen)
            {
                int index = _ElementGrid.ItemContainerGenerator.IndexFromContainer(row);
                UpdateGridColours(index);
            }

        }

        private void UpdateGridColours(Element element)
        {
            int index = elements.IndexOf(element);
            UpdateGridColours(index);
        }

        private void UpdateGridColours(int index)
        {
            if (index < elements.Count)
            {
                Element element = elements[index];
                _ElementGrid.UpdateLayout();
                //UpdateLayout();
                DataGridRow row = (DataGridRow)_ElementGrid.ItemContainerGenerator.ContainerFromIndex(index);
                if (index == 0)
                {
                    Debug.WriteLine("0");
                }
                if (row != null)
                {
                    if (element.BrickLinkPrice > element.LegoPrice && element.LegoPrice != 0.0)
                    {
                        row.Background = Brushes.LightGreen;
                    }
                }
            }
        }

        private async Task<HtmlDocument> GetHtmlDocument(string url){
            // Asynchronously fetch the data from the URL
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();

            // Load the HTML content into an HtmlDocument
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);
            return htmlDoc;
        }

        private void AllowUIToUpdate()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(delegate (object parameter)
            {
                frame.Continue = false;
                return null;
            }), null);

            Dispatcher.PushFrame(frame);
            //EDIT:
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                                          new Action(delegate { }));
        }
        private static async Task<string> CallUrl(string fullUrl)
        {
            HttpClient client = new HttpClient();
            var response = await client.GetStringAsync(fullUrl).ConfigureAwait(false);
            return response;
        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _ElementGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            _ElementGrid.Columns[_ElementGrid.Columns.Count - 1].Visibility = Visibility.Collapsed;
        }
    }
}
