using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Globalization;

namespace Library {
    public partial class Form1 : Form {
        DateTime chosenDate;
        ChromeDriver? driver;
        Table table = new Table();
        public Form1() {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e) {
            button1.Text = "Loading...";
            button1.Enabled = false;
            string rollNo = textBox1.Text.Trim().ToUpper();
            parseChosenDate();
            if (driver != null) driver.Quit();
            _ = execute(rollNo);
        }

        private async Task execute(string rollNo) {
            try {
                driver = new ChromeDriver();
                driver.Manage().Window.Maximize();
                driver.Navigate().GoToUrl("https://alvasdc.easylib.net/index.php/memberController");
                driver.FindElement(By.XPath("//*[@id=\"right\"]/div[1]/span")).Click();
                driver.FindElement(By.XPath("//*[@id=\"right\"]/div[1]/div/a[2]")).Click();
                await WaitForElementVisibility(driver.FindElement(By.XPath("//*[@id=\"myModalMember\"]/div/div/form/div[2]/div[1]/div/input")), TimeSpan.FromSeconds(10));
                driver.FindElement(By.XPath("//*[@id=\"myModalMember\"]/div/div/form/div[2]/div[1]/div/input")).SendKeys(rollNo);
                driver.FindElement(By.XPath("//*[@id=\"myModalMember\"]/div/div/form/div[2]/div[2]/div/input")).SendKeys(rollNo);
                driver.FindElement(By.XPath("//*[@id=\"myModalMember\"]/div/div/form/div[3]/div/button[1]")).Click();
                if (elementExists(By.ClassName("text-danger"))) {
                    displayError("Invalid Username or Password!");
                    driver.Quit();
                    return;
                }
                await WaitForElementVisibility(driver.FindElement(By.XPath("//*[@id=\"allProfiles\"]/a[2]/b/u")), TimeSpan.FromSeconds(10));
                driver.FindElement(By.XPath("//*[@id=\"allProfiles\"]/a[2]/b/u")).Click();
                Thread.Sleep(500);
                driver.FindElement(By.XPath("//*[@id=\"inOutHistoryMember\"]")).Click();
                Thread.Sleep(1000);
                driver.FindElement(By.XPath("//*[@id=\"inoutHistoryTable_length\"]/label/select")).Click();
                driver.FindElement(By.XPath("//*[@id=\"inoutHistoryTable_length\"]/label/select/option[2]")).Click();
                await WaitForElementVisibility(driver.FindElement(By.XPath("//*[@id=\"inoutHistoryTableBody\"]/tr[1]")), TimeSpan.FromSeconds(10));

                table.clear();
                while (true) {
                    getEntriesInPage();
                    if (driver.FindElement(By.ClassName("next")).GetAttribute("class").Contains("disabled")) break;
                    else driver.FindElement(By.ClassName("next")).Click();
                }
                this.WindowState = FormWindowState.Minimized;
                this.Show();
                this.WindowState = FormWindowState.Normal;
                MessageBox.Show($"Attendance from { chosenDate.ToString("dd-MMM-yyyy") } is: { table.getCount() } hours", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                button1.Text = "Generate";
                button1.Enabled = true;
            } catch (Exception ex) {
                displayError(ex.Message);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                driver.Quit();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }
        private void getEntriesInPage() {
            int i = 1;
            string inTime, outTime, date;
            DateTime inT, outT;
            Entry currentEntry;
            while (true) {
                if (elementExists(By.XPath("//*[@id=\"inoutHistoryTableBody\"]/tr[" + i + "]"))) {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    date = driver.FindElement(By.XPath("//*[@id=\"inoutHistoryTableBody\"]/tr[" + i + "]")).FindElements(By.TagName("td"))[1].Text;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    if (DateTime.ParseExact(date, "dd-MMM-yyyy", CultureInfo.InvariantCulture) >= chosenDate) {
                        inTime = driver.FindElement(By.XPath("//*[@id=\"inoutHistoryTableBody\"]/tr[" + i + "]")).FindElements(By.TagName("td"))[2].Text;
                        outTime = driver.FindElement(By.XPath("//*[@id=\"inoutHistoryTableBody\"]/tr[" + i + "]")).FindElements(By.TagName("td"))[3].Text;
                        if (DateTime.TryParseExact(inTime, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out inT) && DateTime.TryParseExact(outTime, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out outT)) {
                            currentEntry = new Entry(DateTime.ParseExact(date, "dd-MMM-yyyy", CultureInfo.InvariantCulture), inT, outT);
                            if (currentEntry.checkIfConsidered()) table.putEntry(currentEntry);
                        }
                    } else break;
                } else break;
                i++;
            }
        }

        private async Task WaitForElementVisibility(IWebElement element, TimeSpan timeout) {
            DateTime endTime = DateTime.Now.Add(timeout);

            while (DateTime.Now < endTime) {
                try {
                    if (element.Displayed) {
                        return;
                    }
                } catch (StaleElementReferenceException) { } catch (NoSuchElementException) { }

                await Task.Delay(500);
            }

            throw new TimeoutException($"Timed out after {timeout.TotalSeconds} seconds waiting for element to be visible.");
        }

        private void displayError(string prompt) {
            this.WindowState = FormWindowState.Minimized;
            this.Show();
            this.WindowState = FormWindowState.Normal;
            MessageBox.Show(prompt, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            button1.Text = "Generate";
            button1.Enabled = true;
        }

        private bool elementExists(By by) {
            try {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                driver.FindElement(by);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                return true;
            } catch (NoSuchElementException) {
                return false;
            }
        }

        private void parseChosenDate() {
            chosenDate = DateTime.ParseExact(dateTimePicker1.Value.ToString("dd-MMM-yyyy"), "dd-MMM-yyyy", CultureInfo.InvariantCulture);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (driver != null) driver.Quit();
        }
    }

    class Entry {
        private DateTime date, inTime, outTime;
        private int consideredInTime = 10, consideredOutTime = 45;
        public Entry(DateTime date, DateTime inTime, DateTime outTime) {
            this.date = date;
            this.inTime = inTime;
            this.outTime = outTime;
        }
        public bool checkIfConsidered() {
            if (inTime.Minute < consideredInTime && outTime.Minute > consideredOutTime) return true;
            if ((outTime - inTime).TotalMinutes >= 50) return true;
            return false;
        }

        public DateTime getDate() {
            return date;
        }
    }

    class Table {
        List<Entry> entries = new List<Entry>();

        public void checkConsideredWithDates() {
            DateTime currentDate;
            for(int i = 0; i < entries.Count; i++) {
                currentDate = entries[i].getDate();
                for(int j = i+1; i < entries.Count; j++) {
                    if (entries[j].getDate() == currentDate) entries.RemoveAt(j);
                }
            }
        }
        public void putEntry(Entry e) {
            entries.Add(e);
        }

        public void clear() {
            entries.Clear();
        }

        internal string getCount() {
            return entries.Count().ToString();
        }
    }
}