# CAuLi #
## Console Audio Library ##
CAuLi is a command line audio player

<img width="969" height="513" alt="image" src="https://github.com/user-attachments/assets/513913f8-3324-45bb-86da-9fe21d26f580" />

<img width="969" height="512" alt="image" src="https://github.com/user-attachments/assets/8e199d47-ced0-4348-b174-7864215beccf" />

**Features:**

* Gapeless playback
* 10 band equalizer (with editable presets)
* Lyrics
* Color themes

### Configuration

Configuration is stored in a XML file.

The configuration file on Windows is in `%UserProfile%\AppData\Local\CAuLi`, on Linux is in `~/.local/share/CauLi`

To configure the folders containing your music files edit the `MusicFolders` value (to add multiple folders separate them with a `;`)

```xml
<ConfigValue Name="MusicFolders">
  <Value>d:\Music;c:\Music</Value>
</ConfigValue>
```
