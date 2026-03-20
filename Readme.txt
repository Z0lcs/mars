
Csapat név: 404 not found
Csapattagok nevei: Arany Zoltán, Herczeg Ákos, Szanics Ferenc
Csapat elérhetőség: aranyzoli11@gmail.com
Iskola: Debreceni SZC Mechwart András Gépipari és Informatikai Technikum
Felkészítő tanár: Hagymási Gyula Levente
Felkészítő tanár email címe: oktato.ber@gmail.com
A programfejlesztői környezet:
Fejlesztőeszköz (IDE): Microsoft Visual Studio 2022 és 2026
Programozási nyelv: C#
Keretrendszer: .NET 8.0
Felhasznált külső könyvtárak: LiveCharts.Wpf (verzió: 0.9.7) – Grafikonok és diagramok megjelenítéséhez.
Célplatform: Windows (WPF Application)

Program használati útmutatója:

A program a FuttathatóFájl nevű mappában található Mars.exe néven. Inditsuk el az exe-t.
Utána első lépésként adjuk meg a térképet .csv formátumban. Válasszuk ki a mappa helyét, ahová a program a log-fájlokat menti (eseménynapló, dashboard adatok, rover útvonala). Emellett állítsuk be a szimuláció hosszát, majd indítsuk el a folyamatot.

A képernyőn a térkép mellett egy HUD segíti az irányítást. A funkciók balról jobbra:
•	Nyomvonal: Ki/be kapcsolhatjuk a rover útvonalának megjelenítését.
•	Sebesség: A csúszkával tetszőlegesen állíthatjuk a szimuláció tempóját.
•	Főmenü: Bármikor kiléphetünk; ekkor a szimuláció leáll, és a következő indításnál tiszta lappal indul.
•	Lejátszás: Ezzel a gombbal indíthatjuk el a mozgást.
•	Manuális léptetés: A [W] billentyű lenyomásával lépésenként is haladhatunk a szimulációban.

A szimuláció közben megfigyelhető egy állapotjelző. Itt követhetjük az eltelt időt, az akkumulátor töltöttségét, a rover aktuális státuszát, a megtett távolságot és az összegyűjtött ásványok számát.
A jobb oldalon találhatjuk a dashboardot. A fülre kattintva lenyithatjuk a részletes adatlapot, ahol minden lépés után percre kész információkat kapunk a történésekről.

Amint a szimuláció véget ér, a "Lejátszás" gomb helyén két új opció jelenik meg:
•	Log megnyitása: Megnyitja a korábban megadott helyre mentett részletes eseménynaplót.
•	Újraindítás: Visszarepít a kezdőpontra a szimuláció újrafuttatásához.
