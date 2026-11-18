
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Win32;

namespace StacjePogodowe
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Pomiar> pomiary =
            new ObservableCollection<Pomiar>();

        private ICollectionView widok;

        public MainWindow()
        {
            InitializeComponent();

            widok = CollectionViewSource.GetDefaultView(pomiary);

            TabelaPomiarow.ItemsSource = widok;

            widok.Filter = FiltrujPomiar;

            AktualizujStacje();

            WczytajAutomatycznie();
        }

        // FILTROWANIE POMIAROW

        private bool FiltrujPomiar(object obiekt)
        {
            Pomiar p = obiekt as Pomiar;

            if (p == null)
                return false;

            string stacja = FiltrStacji.SelectedItem as string;

            if (string.IsNullOrEmpty(stacja) ||
                stacja == "Wszystkie stacje")
                return true;

            return p.Stacja == stacja;
        }

        // ODSWIEZANIE LISTY STACJI

        private void AktualizujStacje()
        {
            string wybrana = FiltrStacji.SelectedItem as string;

            var stacje = pomiary
                .Select(p => p.Stacja)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            stacje.Insert(0, "Wszystkie stacje");

            FiltrStacji.ItemsSource = stacje;

            if (wybrana != null && stacje.Contains(wybrana))
            {
                FiltrStacji.SelectedItem = wybrana;
            }
            else
            {
                FiltrStacji.SelectedIndex = 0;
            }

            
        }

        // ZMIANA WYBRANEJ STACJI

        private void FiltrStacji_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (widok != null)
            {
                widok.Refresh();
                AktualizujKolory();
            }

        }



        // POKAZANIE WSZYSTKICH STACJI

        private void Wszystkie_Click(
            object sender, RoutedEventArgs e)
        {
            FiltrStacji.SelectedIndex = 0;
        }

        // OBSLUGA ZMIAN W POMIARZE

        private void DodajObsluge(Pomiar p)
        {
            p.PropertyChanged += Pomiar_PropertyChanged;
        }

        private void Pomiar_PropertyChanged(
            object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Pomiar.Stacja) ||
                e.PropertyName == nameof(Pomiar.Temperatura) ||
                e.PropertyName == nameof(Pomiar.Wilgotnosc) ||
                e.PropertyName == nameof(Pomiar.Cisnienie))
            {
                Pomiar p = sender as Pomiar;

                if (p != null)
                    p.CzyZmieniony = true;

                AktualizujKolory();

                if (e.PropertyName == nameof(Pomiar.Stacja))
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        AktualizujStacje();
                    }), DispatcherPriority.Background);
                }
            }
        }

        // AUTOMATYCZNE WCZYTYWANIE

        private void WczytajAutomatycznie()
        {
            string sciezka = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "pomiary.txt");

            if (File.Exists(sciezka))
            {
                WczytajZPliku(sciezka);
            }
            else
            {
                Informacja.Text = "Nie znaleziono pliku pomiary.txt";
            }
        }

        // WCZYTYWANIE DANYCH Z PLIKU

        private void WczytajZPliku(string sciezka)
        {
            try
            {
                pomiary.Clear();

                string[] linie = File.ReadAllLines(sciezka);

                foreach (string linia in linie)
                {
                    if (string.IsNullOrWhiteSpace(linia))
                        continue;

                    string[] dane = linia.Split(';');

                    if (dane.Length != 5)
                        continue;

                    string stacja = dane[0].Trim();

                    DateTime data = DateTime.MinValue;
                    double temperatura = 0;
                    int wilgotnosc = 0;
                    int cisnienie = 0;

                    bool poprawne =
                        DateTime.TryParseExact(
                            dane[1].Trim(),
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out data)
                        &&
                        double.TryParse(
                            dane[2].Trim(),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out temperatura)
                        &&
                        int.TryParse(
                            dane[3].Trim(),
                            out wilgotnosc)
                        &&
                        int.TryParse(
                            dane[4].Trim(),
                            out cisnienie);

                    if (!poprawne)
                        continue;

                    Pomiar nowy = new Pomiar(
                        stacja,
                        data,
                        temperatura,
                        wilgotnosc,
                        cisnienie);

                    DodajObsluge(nowy);

                    pomiary.Add(nowy);
                }

                AktualizujStacje();
                AktualizujKolory();

                Informacja.Text =
                    "Wczytano pomiary: " + pomiary.Count;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Blad wczytywania pliku: " + ex.Message);
            }
        }

        // PRZYCISK WCZYTAJ

        private void Wczytaj_Click(
            object sender, RoutedEventArgs e)
        {
            OpenFileDialog okno = new OpenFileDialog();

            okno.Filter = "Pliki tekstowe (*.txt)|*.txt";

            if (okno.ShowDialog() == true)
            {
                WczytajZPliku(okno.FileName);
            }
        }

        // ZAPISYWANIE DO PLIKU

        private void Zapisz_Click(
            object sender, RoutedEventArgs e)
        {
            SaveFileDialog okno = new SaveFileDialog();

            okno.Filter = "Pliki tekstowe (*.txt)|*.txt";
            okno.FileName = "pomiary.txt";

            if (okno.ShowDialog() == true)
            {
                try
                {
                    using (StreamWriter sw =
                        new StreamWriter(okno.FileName))
                    {
                        sw.WriteLine(
                            "Stacja;Data;Temperatura;Wilgotnosc;Cisnienie");

                        foreach (Pomiar p in pomiary)
                        {
                            sw.WriteLine(
                                p.Stacja + ";" +
                                p.Data.ToString(
                                    "yyyy-MM-dd",
                                    CultureInfo.InvariantCulture) + ";" +
                                p.Temperatura.ToString(
                                    CultureInfo.InvariantCulture) + ";" +
                                p.Wilgotnosc + ";" +
                                p.Cisnienie);
                        }
                    }

                    foreach (Pomiar p in pomiary)
                    {
                        p.CzyZmieniony = false;
                    }

                    AktualizujKolory();

                    Informacja.Text = "Zapisano dane do pliku.";

                    MessageBox.Show("Dane zostaly zapisane.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Blad zapisu: " + ex.Message);
                }
            }
        }

        // DODAWANIE NOWEGO POMIARU

        private void Dodaj_Click(
            object sender, RoutedEventArgs e)
        {
            string stacja = NowaStacja.Text.Trim();

            double temperatura = 0;
            int wilgotnosc = 0;
            int cisnienie = 0;

            if (string.IsNullOrWhiteSpace(stacja))
            {
                MessageBox.Show("Podaj nazwe stacji.");
                return;
            }

            bool tempOK = double.TryParse(
                NowaTemperatura.Text.Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out temperatura);

            bool wilgOK = int.TryParse(
                NowaWilgotnosc.Text,
                out wilgotnosc);

            bool cisOK = int.TryParse(
                NoweCisnienie.Text,
                out cisnienie);

            if (!tempOK || !wilgOK || !cisOK)
            {
                MessageBox.Show(
                    "Wprowadz poprawne dane pomiaru.");
                return;
            }

            if (wilgotnosc < 0 || wilgotnosc > 100 ||
                cisnienie <= 0)
            {
                MessageBox.Show(
                    "Niepoprawna wilgotnosc lub cisnienie.");
                return;
            }

            Pomiar nowy = new Pomiar(
                stacja,
                DateTime.Today,
                temperatura,
                wilgotnosc,
                cisnienie);

            DodajObsluge(nowy);

            nowy.CzyZmieniony = true;

            pomiary.Add(nowy);

            NowaStacja.Clear();
            NowaTemperatura.Clear();
            NowaWilgotnosc.Clear();
            NoweCisnienie.Clear();

            FiltrStacji.SelectedIndex = 0;

            AktualizujStacje();
            AktualizujKolory();

            Informacja.Text = "Dodano nowy pomiar.";
        }

        // WYSZUKIWANIE TEMPERATURY MAX I MIN

        private void AktualizujKolory()
        {
            var widoczne = pomiary
                .Where(p => FiltrujPomiar(p))
                .ToList();

            foreach (Pomiar p in pomiary)
            {
                p.CzyMax = false;
                p.CzyMin = false;
            }

            if (widoczne.Count == 0)
                return;

            double max = widoczne.Max(p => p.Temperatura);
            double min = widoczne.Min(p => p.Temperatura);

            foreach (Pomiar p in widoczne)
            {
                if (p.Temperatura == max)
                    p.CzyMax = true;

                if (p.Temperatura == min)
                    p.CzyMin = true;
            }
        }

        // ZMIANA DANYCH W TABELI


        private void TabelaPomiarow_CellEditEnding(
            object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (TabelaPomiarow.CommitEdit(DataGridEditingUnit.Row, true))
                {
                    AktualizujStacje();
                    AktualizujKolory();
                    TabelaPomiarow.Items.Refresh();
                }

            }), DispatcherPriority.Background);
        }

    }

    // KLASA POMIAR

    public class Pomiar : INotifyPropertyChanged
    {
        private string stacja = "";
        private DateTime data;
        private double temperatura;
        private int wilgotnosc;
        private int cisnienie;

        private bool czyMax;
        private bool czyMin;
        private bool czyZmieniony;

        public event PropertyChangedEventHandler PropertyChanged;

        public Pomiar(
            string stacja,
            DateTime data,
            double temperatura,
            int wilgotnosc,
            int cisnienie)
        {
            this.stacja = stacja;
            this.data = data;
            this.temperatura = temperatura;
            this.wilgotnosc = wilgotnosc;
            this.cisnienie = cisnienie;
        }

        public string Stacja
        {
            get { return stacja; }
            set
            {
                if (stacja != value)
                {
                    stacja = value;
                    OnPropertyChanged(nameof(Stacja));
                }
            }
        }

        public DateTime Data
        {
            get { return data; }
            set
            {
                if (data != value)
                {
                    data = value;
                    OnPropertyChanged(nameof(Data));
                }
            }
        }

        public double Temperatura
        {
            get { return temperatura; }
            set
            {
                if (temperatura != value)
                {
                    temperatura = value;
                    OnPropertyChanged(nameof(Temperatura));
                }
            }
        }

        public int Wilgotnosc
        {
            get { return wilgotnosc; }
            set
            {
                if (wilgotnosc != value)
                {
                    wilgotnosc = value;
                    OnPropertyChanged(nameof(Wilgotnosc));
                }
            }
        }

        public int Cisnienie
        {
            get { return cisnienie; }
            set
            {
                if (cisnienie != value)
                {
                    cisnienie = value;
                    OnPropertyChanged(nameof(Cisnienie));
                }
            }
        }

        public bool CzyMax
        {
            get { return czyMax; }
            set
            {
                if (czyMax != value)
                {
                    czyMax = value;
                    OnPropertyChanged(nameof(CzyMax));
                }
            }
        }

        public bool CzyMin
        {
            get { return czyMin; }
            set
            {
                if (czyMin != value)
                {
                    czyMin = value;
                    OnPropertyChanged(nameof(CzyMin));
                }
            }
        }

        public bool CzyZmieniony
        {
            get { return czyZmieniony; }
            set
            {
                if (czyZmieniony != value)
                {
                    czyZmieniony = value;
                    OnPropertyChanged(nameof(CzyZmieniony));
                }
            }
        }

        private void OnPropertyChanged(string nazwa)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nazwa));
        }
    }
}
