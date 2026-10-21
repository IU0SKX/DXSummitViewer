# Dx Summit Viewer

Applicazione per Windows che mostra in tempo reale gli spot del cluster [DX Summit](https://www.dxsummit.fi/), con filtri per paese, nominativo, banda e modo. Permette di inviare nuovi spot e di sintonizzare la radio su uno spot con un doppio clic, tramite RigCtld.

Un solo file `.exe` di circa 150 KB, senza installazione e senza dipendenze esterne.

![Dx Summit Viewer – tema chiaro](docs/screenshot-light.png)

<details>
<summary>Tema scuro</summary>

![Dx Summit Viewer – tema scuro](docs/screenshot-dark.png)

</details>

## Funzionalità

- **Lista autoaggiornante**: gli spot si aggiornano da soli (15 s, 30 s, 1 min, 2 min, 5 min o 10 min) oppure con il pulsante *Aggiorna ora*. Gli spot arrivati con l'ultimo aggiornamento sono evidenziati.
- **Filtro per paese**: menu con i paesi presenti negli spot, con suggerimento mentre si scrive. Accetta anche i nomi in italiano (*Italia*, *Sicilia*, *Sardegna*). Selezionando *Italy* vengono inclusi anche Sicily, Sardinia e African Italy.
- **Ricerca per nominativo**: cerca nel nominativo DX e, attivando *anche spotter*, anche tra chi ha inviato lo spot.
- **Filtro per banda**: da 2200m a 23cm.
- **Filtro per modo**: CW, SSB, FT8, FT4, RTTY, PSK, Digi, FM, AM, SSTV.
- **Invio di spot** a DX Summit, con conferma prima dell'invio. Il proprio nominativo viene ricordato.
- **Controllo della radio via RigCtld**: con un doppio clic su uno spot, la radio si porta sulla sua frequenza e sul suo modo tramite un server RigCtld (Hamlib `rigctld`, vfview o altri programmi compatibili). Vedi [Controllo radio](#controllo-radio-rigctld).
- **Menu con il tasto destro** su uno spot: sintonizza la radio, copia nominativo o frequenza, apri il nominativo su QRZ.com, spotta di nuovo lo stesso DX, filtra per quel paese. Se il controllo radio è disattivato, il doppio clic copia il nominativo.
- **Tema chiaro e scuro** (icona sole/luna), con la scelta ricordata al successivo avvio.
- Colonne ordinabili cliccando sull'intestazione. Lo spot selezionato resta selezionato anche dopo gli aggiornamenti.

## Requisiti

- Windows 10 o Windows 11
- .NET Framework 4.x, già incluso in Windows 10 e 11
- Connessione a internet

## Installazione

1. Scaricare `DXSummitViewer.exe` dalla pagina [Releases](../../releases).
2. Avviarlo con un doppio clic. Non serve installare nulla.

> **Nota:** l'eseguibile non ha una firma digitale, quindi al primo avvio Windows SmartScreen potrebbe mostrare l'avviso *"Windows ha protetto il PC"*. Per avviarlo comunque: *Ulteriori informazioni* → *Esegui comunque*.

## Controllo radio (RigCtld)

L'app può comandare la radio tramite il protocollo di rete di [Hamlib](https://hamlib.github.io/) `rigctld`. Lo usano direttamente `rigctld` e i programmi che espongono un server compatibile, come vfview.

1. Avviare il server RigCtld (`rigctld`, vfview…) collegato alla radio.
2. Nell'app, cliccare l'icona a ingranaggio e inserire indirizzo IP e porta del server (default `127.0.0.1` e `4532`).
3. *Prova connessione* verifica il collegamento e mostra la frequenza attuale della radio.
4. Attivare *Sintonizza la radio con il doppio clic* e salvare.

Da quel momento il doppio clic su uno spot (o *Sintonizza la radio* nel menu del tasto destro) imposta sulla radio il modo dello spot (`M <modo> 0`) e poi la sua frequenza (`F <frequenza in Hz>`). L'esito compare nella barra di stato in basso.

| Modo dello spot | Modo impostato sulla radio |
|---|---|
| SSB su 160, 80 e 40 m | LSB |
| SSB sulle altre bande (60 m compresi) | USB |
| CW, FM, AM, RTTY | lo stesso modo |
| FT8, FT4, PSK, altri digitali | USB-Data (`PKTUSB`), oppure USB se la radio non lo supporta |
| SSTV | USB |
| modo non riconosciuto | invariato (viene impostata solo la frequenza) |

Il modo viene impostato prima della frequenza perché su alcune radio il cambio di modo sposta leggermente la frequenza. Il modo dello spot è ricavato dal commento o dal band plan (vedi sotto), quindi in qualche caso può non corrispondere a quello effettivo.

## Come vengono determinati banda e modo

L'API di DX Summit fornisce la frequenza ma non il modo di emissione.

- La **banda** viene calcolata dalla frequenza.
- Il **modo** viene ricavato in quest'ordine:
  1. dalle parole chiave nel commento dello spot (`FT8`, `CW`, `SSB`, `RTTY`…);
  2. dalle frequenze standard FT8 e FT4;
  3. dal band plan IARU.

Per gli spot senza indicazioni nel commento il modo è quindi una stima.

## Impostazioni salvate

Il programma salva solo il proprio nominativo (per l'invio degli spot), il tema scelto e le impostazioni di RigCtld (indirizzo, porta, attivazione), in file di testo nella cartella:

```
%APPDATA%\DXSummitViewer\
```

## Compilare dai sorgenti

Non servono Visual Studio né il .NET SDK: basta il compilatore C# incluso in .NET Framework 4.x, presente in ogni installazione di Windows.

```bat
build.bat
```

Lo script produce `DXSummitViewer.exe` nella stessa cartella.

Per modificare l'icona, cambiare il disegno in `MakeIcon.cs` e rigenerare `app.ico` prima di ricompilare:

```bat
make_icon.bat
build.bat
```

### Struttura del progetto

| File | Contenuto |
|---|---|
| `DXSummitViewer.cs` | Finestra principale, lettura e filtro degli spot, invio spot, tema, finestre Impostazioni e Informazioni |
| `UiControls.cs` | Controlli disegnati a mano che seguono il tema (menu a tendina, campi, interruttori, pulsanti, icone) |
| `RigCtl.cs` | Client del protocollo RigCtld per sintonizzare la radio |
| `MakeIcon.cs` | Generatore dell'icona `app.ico` |
| `app.ico` | Icona dell'applicazione (16–256 px) |
| `build.bat` | Compila l'eseguibile |
| `make_icon.bat` | Rigenera l'icona |
| `docs/` | Screenshot usati in questo README |

## Fonte dei dati

Gli spot provengono dall'API pubblica di DX Summit:

- `GET https://new.dxsummit.fi/api/v1/spots?limit=N`: elenco degli spot più recenti
- `POST https://new.dxsummit.fi/api/v1/spots`: invio di un nuovo spot (`de_call`, `dx_call`, `frequency`, `info`)

Questo progetto non è affiliato a DX Summit né a OH8X.

## Crediti

- Dati: [DX Summit](https://www.dxsummit.fi/) di OH8X
- Icona dell'applicazione e icona *info*: basate sulle icone [Lucide](https://lucide.dev/) (licenza ISC)
- Controllo radio: protocollo di rete di [Hamlib](https://hamlib.github.io/) `rigctld`

## Autore

**IU0SKX**: segnalazioni e suggerimenti sono benvenuti tramite le [Issues](../../issues).

73!
