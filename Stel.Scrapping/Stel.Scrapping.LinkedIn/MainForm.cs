using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using System.Threading;
using Newtonsoft.Json;

namespace Stel.Scrapping.LinkedIn
{
    public partial class MainForm : Form
    {
        private Status _curStatus = Status.Paused;
        private ChromiumWebBrowser _MyBrowser = null;
        private string[] _contactsLinks = null;
        private int _currentIndex = 0;
        private List<Contact> _ContactList = null;
        private bool _flagFirsttLoad = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadConfiguration();
            InitializeBrouser();
            Cef.Initialize();
        }

        private void InitializeBrouser()
        {


            //webBrowser1.Navigating += WebBrowser1Navigating;
            //webBrowser1.Navigated += webBrowser1_Navigated;
            //webBrowser1.ProgressChanged += webBrowser1_ProgressChanged;
            Log(Helper.GetEnumDescription<Status>(_curStatus));
        }

        void webBrowser1_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {

        }

        void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            if (!webBrowser1.IsBusy)
            {
                StopLoadingInProgress();
            }
        }

        private void StopLoadingInProgress()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() =>
                                                  {
                                                      progressBar1.MarqueeAnimationSpeed = 0;
                                                      progressBar1.Style = ProgressBarStyle.Blocks;
                                                  }));

            }
            else
            {
                progressBar1.MarqueeAnimationSpeed = 0;
                progressBar1.Style = ProgressBarStyle.Blocks;
            }

        }

        private void StarLoadingInProgress()
        {
            progressBar1.MarqueeAnimationSpeed = 80;
            progressBar1.Style = ProgressBarStyle.Marquee;
            Log("Loading...");
        }

        private void Log(string msg)
        {
            var rMsg = string.Format("{0}{1}", msg, Environment.NewLine);
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => txtLogs.AppendText(rMsg)));

            }
            else
            {
                txtLogs.AppendText(rMsg);
            }

        }

        private void ClearLog()
        {
            txtLogs.Clear();
        }

        void WebBrowser1Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            StarLoadingInProgress();
        }

        private void LoadConfiguration()
        {
            txtUserId.Text = Helper.GetAppSettingAsString("User");
            txtUserPassword.Text = Helper.GetAppSettingAsString("Password");
            txtServerUrl.Text = Helper.GetAppSettingAsString("ServerUrl");
            txtAuthUrl.Text = Helper.GetAppSettingAsString("AuthUrl");
            txtPath.Text = Helper.GetAppSettingAsString("FilePath");
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                Clear();
                _ContactList = new List<Contact>();
                this.panelBrowser.Controls.Clear();
                StarLoadingInProgress();
                _curStatus = Status.Loading;
                _MyBrowser = new ChromiumWebBrowser(txtAuthUrl.Text);
                this.panelBrowser.Controls.Add(_MyBrowser);
                _MyBrowser.Dock = DockStyle.Fill;
                _MyBrowser.LoadingStateChanged += _MyBrowser_LoadingStateChanged;
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void Clear()
        {
            _currentIndex = 0;
            _contactsLinks = null;
            txtLogs.Clear();
        }

        void _MyBrowser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {

                switch (_curStatus)
                {
                    case Status.Loading:
                        _curStatus = Status.Loggin;
                        LoginUser();
                        break;

                    case Status.Loggin:
                        _curStatus = Status.Searching;
                        GoConnectionsPage();
                        break;

                    case Status.Searching:
                        _curStatus = Status.GettingContactInfo;
                        GetContactsLinks();
                        break;

                    case Status.GettingContactInfo:
                        GetCurrentContactInfo();
                        break;

                    case Status.Completed:
                        _curStatus = Status.Completed;
                        break;

                }
                Log(Helper.GetEnumDescription<Status>(_curStatus));

            }
            else
            {
                // StopLoadingInProgress();
            }
        }

        private void GetCurrentContactInfo()
        {
            if(_flagFirsttLoad)
            {
                var scroolScript = @"(function() {                                        
                                    window.scrollTo(0, document.body.scrollHeight || document.documentElement.scrollHeight);
                               })();";


                 _MyBrowser.ExecuteScriptAsync(scroolScript);
                _flagFirsttLoad = false;
                Thread.Sleep(2000);
                return;
            }
        
            var scriptTmpl = @"(function() {         
                                     var result = '', contactName = '', title = '', curWork='', passWork= '', education = '', email = '', linkedin = '', location = '';                               
                                      try {contactName =  document.getElementById('name').getElementsByClassName('full-name')[0].innerText; }catch(err){};
                                      try {title = document.getElementById('headline').getElementsByTagName('p')[0].innerText; }catch(err){};
                                      try {curWork = document.getElementById('overview-summary-current').getElementsByTagName('a')[2].innerText; }catch(err){};
                                      try {passWork = document.getElementById('overview-summary-past').getElementsByTagName('a')[2].innerText; }catch(err){};    
                                      try {education = document.getElementById('overview-summary-education').getElementsByTagName('a')[2].innerText; }catch(err){};
                                      try {email = document.getElementById('email-view').getElementsByTagName('a')[0].innerText; }catch(err){};
                                      try {linkedin = document.getElementsByClassName('view-public-profile')[0].innerText; }catch(err){};
                                      try {location = document.getElementById('location').getElementsByTagName('a')[0].innerText;}catch(err){};
                                   
                                     var skillString = ''; 
                                 try {
                                     var skillsTags = document.getElementsByClassName('endorse-item-name-text');
                                     for (i = 0; i < skillsTags.length; i++) {
                                         skillString += skillsTags[i].innerText;
                                        if(i <  skillsTags.length -1)
                                          skillString += ', '; 
                                      } 
                                    }catch(err){};
                                     var contactsString = '';   
                                    try{         
                                     var contacts = document.getElementsByClassName('connections-name');
                                     for (i = 0; i < contacts.length; i++) {
                                         contactsString += contacts[i].innerText;
                                        if(i <  contactsString.length -1)
                                          contactsString += ', '; 
                                      } 
                                 }catch(err){};
                                    result = { 'Name': contactName, 'Email':email, 'CurrentWork':curWork, 'PreviousWork':passWork, 'Title':title, 'Education':education, 'Location':location, 'Linkedin':linkedin, 'Skills': skillString, 'Contacts':contactsString } 

                                    return JSON.stringify(result);
                                })();";

            var script = scriptTmpl;
            _MyBrowser.EvaluateScriptAsync(script).ContinueWith(t =>
            {
                if (!t.IsFaulted)
                {
                    var curContactString = t.Result.Result.ToString();
                    var curContactObject = JsonConvert.DeserializeObject<Contact>(curContactString);
                    _ContactList.Add(curContactObject);
                    _currentIndex++;
                    ProssessContactstLinks();
                }
            });
            
        }

        private void UpdateGrid()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => {
                    dgvData.DataSource = _ContactList;
                }));
            }
            else
            {
                dgvData.DataSource = _ContactList;
            }
        }

        private void GetContactsLinks()
        {
            var scriptTmpl = @"(function() {         
                                     var result = '';                               
                                     var contactList =  document.getElementsByClassName('contact-item-view');
                                     for (i = 0; i < contactList.length; i++) {
                                         var curTag = contactList[i].getElementsByClassName('image');
                                         if(curTag)
                                          result += curTag[0].href + '|';  
                                      } 
                                
                                    return result;
                                })();";

            var script = scriptTmpl;
            _MyBrowser.EvaluateScriptAsync(script).ContinueWith(t =>
                                                                    {
                                                                        if (!t.IsFaulted)
                                                                        {
                                                                            Log(t.Result.Result.ToString());
                                                                            var stringResult =
                                                                                t.Result.Result.ToString().Split('|');

                                                                            ParseResult(stringResult);
                                                                        }
                                                                    });


        }

        private void ParseResult(string[] stringResult)
        {
            if (stringResult.Length > 0)
            {
                _contactsLinks = stringResult;
                _currentIndex = 0;
                ProssessContactstLinks();
            }
        }

        private void GedIdsFromUrls(string[] hrefList)
        {
            List<string> contactsIds = new List<string>();
            var pResult = string.Empty;
            foreach (var href in hrefList)
            {
                pResult = getConIdFromUrl(href);
                if (!string.IsNullOrEmpty(pResult) && !string.IsNullOrWhiteSpace(pResult))
                    contactsIds.Add(pResult);
            }

            /// ProssessContactstLinks(contactsIds);
        }

        private void ProssessContactstLinks()
        {
           
            if (_currentIndex < _contactsLinks.Length)
            {
                _flagFirsttLoad = true;
                if (!string.IsNullOrEmpty(_contactsLinks[_currentIndex]))
                {
                    _MyBrowser.Load(_contactsLinks[_currentIndex]);
                }
                else
                {
                    _currentIndex++;
                    ProssessContactstLinks();
                }
            }
            else
            {
                _curStatus = Status.Completed;
                Log(Helper.GetEnumDescription<Status>(_curStatus));
                StopLoadingInProgress();
                UpdateGrid();
                SaveData();
                MessageBox.Show("Done....");
            }
        }

        private void SaveData()
        {
            try
            {
                if (!string.IsNullOrEmpty(this.txtPath.Text))
                {
                    Helper.WriteCSV<Contact>(_ContactList, this.txtPath.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private string getConIdFromUrl(string href)
        {
            //Todo:// Use Regex Match to search id.
            string result = "";
            string[] spearatorOne = { "id=li_" };
            string[] spearatorTwo = { "&" };
            var partOne = href.Split(spearatorOne, StringSplitOptions.None);
            if (partOne.Length > 1)
            {
                var partTwo = partOne[1].Split(spearatorTwo, StringSplitOptions.None);
                if (partTwo.Length > 0)
                    result = partTwo[0];
            }

            return result;
        }

        private void GoConnectionsPage()
        {
            var curUrl =
                string.Format("{0}contacts#?sortOrder=recent&fromFilter=true&connections=enabled&source=LinkedIn&",
                              txtServerUrl.Text);
            _MyBrowser.Load(curUrl);
        }



        private void LoginUser()
        {
            var scriptTmpl = @"(function() {                                        
                                    var elemUser = document.getElementById('session_key-login');
                                    elemUser.value = '{0}';
                                    var elemPass = document.getElementById('session_password-login');
                                    elemPass.value = '{1}';
                                    document.getElementById('btn-primary').click();
                               })();";

            var script = scriptTmpl.Replace("{0}", txtUserId.Text).Replace("{1}", txtUserPassword.Text);
            _MyBrowser.ExecuteScriptAsync(script);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cef.Shutdown();
            Application.Exit();
        }


    }
}
