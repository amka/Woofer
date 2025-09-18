<div align="center">
  <img src="data/icons/hicolor/256x256/com.tenderowl.woofer.png" alt="Woofer Logo" width="128"/>
  <h1>Woofer</h1>
  <p><strong>Modern, fast, and beautiful music player for Linux desktops</strong></p>
</div>

---

## 🎵 Overview
Woofer is a sleek, open-source music player designed for Linux. It offers a clean interface, fast performance, and all the features you need to enjoy your music collection.

## ✨ Features
- Beautiful GTK UI
- Fast music library scanning
- Playlist management
- Cover art extraction
- Grid and list views for tracks
- Support for common audio formats
- Lightweight and responsive

## 🚀 Installation

### Flatpak (Recommended)
Coming soon!

### Build from Source
1. Install dependencies:
   - .NET 9.0 SDK
   - Meson
   - GTK 4
2. Clone the repo:
   ```sh
   git clone https://github.com/tenderowl/woofer.git
   cd woofer
   ```
3. Build:
   ```sh
   dotnet build
   ```
4. Run:
   ```sh
   dotnet run
   ```

## 🛠️ Development

### Prerequisites
- .NET 9.0 SDK
- Meson build system
- GTK 4 development libraries

### Building
```sh
meson setup _build
meson compile -C _build
```

### Project Structure
- Woofer — Main source code
  - `Models/` — Data models (Track, Playlist, etc.)
  - `Services/` — Core logic (Player, Scanner, etc.)
  - `Ui/` — UI components
- data — Resources, icons, schemas
- build-aux — Build helpers
- po — Translations

### Running
```sh
./_build/Woofer/Woofer
```

### Testing

```sh
dotnet test
```

## 🤝 Contributing
We welcome contributions! Please:
- Fork the repository
- Create a feature branch
- Follow C# and GTK best practices
- Submit a pull request with a clear description
- Be respectful and constructive in discussions

See CONTRIBUTING.md for more details.

## 📄 License
MIT — see LICENSE for details.

## Thanks

- [GirCore](https://gircore.github.io/)
- [Thiings Icons](https://www.thiings.co/things)
- [Phosphor Icons](https://phosphoricons.com/)

---

<div align="center">
  <sub>Made with ❤️ by TenderOwl</sub>
</div>
