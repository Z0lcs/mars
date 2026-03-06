1. GetAsvanyok() - A Készlet-összesítő
C#
private List<Point> GetAsvanyok() => terkep.Vizjeg.Concat(terkep.RitkaArany).Concat(terkep.RitkaAsvany).ToList();
Mit csinál? Összegyűjti a térképen lévő összes ásványt egyetlen, közös listába.

Hogyan működik? A C# LINQ nevű funkcióját használja. A Concat paranccsal egyszerűen egymás mögé fűzi a vízjég, az arany és a ritka ásványok listáit.

Miért van rá szükség? Amikor a rover a következő célpontot keresi, nem érdekli, hogy az éppen víz vagy arany, csak a legközelebbi vagy legsűrűbb pontot akarja megtalálni. Ezzel a függvénnyel nem kell háromszor lefuttatni a kereső algoritmust.

2. Tavolsag(Point p1, Point p2) - A Távolságmérő
C#
private int Tavolsag(Point p1, Point p2) => Math.Max(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
Mit csinál? Kiszámolja, hogy hány lépésbe kerül eljutni az egyik pontból a másikba.

Hogyan működik? Ez a matematikai képlet a Csebisev-távolság (sakktábla-távolság). Kiszámolja az X tengelyen és az Y tengelyen lévő különbséget, majd a kettő közül a nagyobbat (Math.Max) adja vissza.

Miért így működik? Mivel a rovered átlósan is tud mozogni, egy átlós lépés is csak 1 lépésnek számít (pl. a 0,0-ról az 1,1-re 1 lépés, nem 2). Ez a képlet ezt tökéletesen leképezi a gép számára.

3. ValasztCelpont(Point akt, List<Point> asvanyok) - A Navigációs Agy
Mit csinál? A rengeteg ásvány közül kiválasztja a legideálisabbat.

Hogyan működik? 1.  Aznnali ellenőrzés: Először megnézi, hogy van-e ásvány a közvetlen szomszédságban (távolság <= 1). Ha talál egyet, nem is gondolkodik tovább, azt adja vissza célpontként.
2.  Klaszterezés: Ha nincs közvetlen szomszéd, minden egyes ásványt lepontoz. Kiszámolja a távolságot (tav), majd megnézi, hogy az adott ásvány 2 blokkos körzetében hány másik ásvány van (suruseg). A képlet (tav - (suruseg * 0.725)) alapján rendezi őket sorba (OrderBy), és a legkisebb pontszámút (vagyis a "legjobb ajánlatot") választja.

Miért zseniális? Ez az egyensúly a közelség és a sűrűség között. A rover hajlandó egy picit messzebb is elmenni, ha cserébe ott 3 ásvány van egymás mellett, de nem megy el a térkép másik végére a nagy zsákmányért, ha a lába előtt is hever egy.

4. ValasztSebesseg(Rover r, Point cel, bool nappal, bool vesz) - A Gázpedál
Mit csinál? Meghatározza a sebességfokozatot a pillanatnyi helyzet alapján.

Hogyan működik? Két fő esetet vizsgál:

Menekülés (vesz == true): Ha menekül, és az akku 35% felett van, akkor 3-as sebességre kapcsol. Ha az akku gyenge, 1-essel megy.

Gyűjtés (vesz == false): Csak akkor adja rá a 3-as sebességet, ha nappal van, és rengeteg az energia (> 40%). Minden más esetben takarékosan, 1-essel halad.

Biztonsági Fék: A függvény legvégén egy Math.Min(Tavolsag, maxSeb) található. Ez biztosítja, hogy ha a rover mondjuk kiszámolta a 3-as sebességet, de a cél csak 1 mezőre van, akkor visszaveszi a sebességet 1-re, hogy ne pazaroljon energiát felesleges sprintelésre.

5. KeresLegjobbSzomszed(Point akt, Point cel) - A Terep-Szenzor
Mit csinál? Ez a lépés-végrehajtó. Megkeresi a 8 szomszédos mező közül azt az egyet, amire lépnie kell a cél felé.

Hogyan működik? Két egymásba ágyazott for ciklussal végignézi a körülötte lévő 3x3-as területet (dx és dy -1-től 1-ig).

Kiszűri a saját helyzetét (0,0).

Ellenőrzi, hogy a vizsgált mező nem lóg-e le a térképről (0-49 között van-e a koordináta).

Ellenőrzi, hogy az adott mezőn nincsenek-e Akadalyok.

Ami átment a teszten, arról kiszámolja, hogy milyen messze van a végső céltól, és a legkisebb távolságút adja vissza.

6. MozgasVegrehajtas(Point cel, int seb) - A Sebességváltó
Mit csinál? Levezényli a többmezős lépéseket egy taktus alatt.

Hogyan működik? Egy for ciklus fut le pontosan annyiszor, amekkora a megadott seb (sebesség) paraméter.

Cikluson belül meghívja a KeresLegjobbSzomszed függvényt egyetlen lépéshez.

Frissíti a rover pozícióját, és számolja a megtett utat.

Ha menet közben rálép a célpontra (rover.Pozicio == cel), a break paranccsal azonnal leállítja a mozgást (hiába "maradt" még benne lépés), így nem fut túl rajta.

Ezek a segédfüggvények dolgoznak együtt, mint egy óramű alkatrészei. A MozgasVegrehajtas hívja a KeresLegjobbSzomszed-et, a ValasztCelpont és a ValasztSebesseg pedig mindketten a Tavolsag-ra támaszkodnak.
