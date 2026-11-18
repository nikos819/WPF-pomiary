#  Stacje Pogodowe (Aplikacja WPF)

Aplikacja desktopowa napisana w języku **C#** z wykorzystaniem technologii **WPF (Windows Presentation Foundation)**, służąca do monitorowania, filtrowania i zarządzania pomiarami pogodowymi z różnych stacji meteorologicznych.

---

##  Główne funkcjonalności

- **Wczytywanie i zapis danych:** Automatyczny odczyt z pliku `pomiary.txt` przy starcie oraz możliwość ręcznego wczytywania i zapisywania plików przez okna dialogowe (`OpenFileDialog` / `SaveFileDialog`).
- **Filtrowanie stacji:** Dynamiczne filtrowanie pomiarów według wybranej stacji meteorologicznej.
- **Wyróżnianie ekstremów:** Automatyczne wyszukiwanie i oznaczanie skrajnych wartości temperatur (minimum i maksimum) w widoku.
- **Dodawanie nowych pomiarów:** Formularz do wprowadzania nowych danych z walidacją poprawności danych (zakres wilgotności, dodatnie ciśnienie, format liczbowy).
- **Edycja w tabeli:** Obsługa edycji danych bezpośrednio w kontrolce `DataGrid` z automatycznym odświeżaniem stanu zmienionych wierszy.

---

##  Format pliku danych (`pomiary.txt`)

Aplikacja operuje na plikach tekstowych rozdzielonych średnikiem (CSV) o następującej strukturze:

```text
Kępno;2026-11-16;-5.2;85;1008
Poznan;2026-11-17;19.5;45;1018
Wrocław;2026-11-17;-11.3;70;1014
Kępno;2026-11-17;7.8;78;1011
