using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Un4seen.Bass;

namespace CAuLi;

public class EqualizerBand
{
  public EqualizerBand()
  {

  }

  public EqualizerBand(float gain)
  {
    Gain = gain;
  }

  public float Gain { get; set; }
}

public class Equalizer
{
  public Equalizer()
  {
    Bands = new List<EqualizerBand>();
  }

  public string Name { get; set; }

  [XmlIgnore]
  public int BandsCount
  {
    get
    {
      if (Bands != null)
        return Bands.Count;
      return 0;
    }
  }

  public List<EqualizerBand> Bands { get; set; }
}

partial class Player
{
  private string _equalizerName = string.Empty;
  private List<int> _equalizer;
  private readonly List<Equalizer> _equalizers = [];

  public delegate void EqualizerChangedHandler(Player sender, string eqName);
  public EqualizerChangedHandler EqualizerChanged;

  public bool EqualizerEnabled
  {
    get
    {
      return _equalizer != null && _equalizer.Count > 0;
    }
  }

  public string EqualizerName
  {
    get { return _equalizerName; }
  }

  public List<Equalizer> Equalizers
  {
    get { return _equalizers; }
  }

  public void LoadStandardEqualizers()
  {
    string eqPath = Path.Combine(Program.RootPath, "Equalizers");
    if (Directory.Exists(eqPath)) {
      foreach (string f in Directory.GetFiles(eqPath, "*.xml")) {
        using (StreamReader sr = new StreamReader(f)) {
          Equalizer eq = Utility.Serialization.Deserialize<Equalizer>(sr.BaseStream);
          if (eq != null)
            _equalizers.Add(eq);
        }
      }
    }
  } // LoadStandardEqualizers

  public void RemoveEqualizer()
  {
    if (_equalizer != null) {
      foreach (int fx in _equalizer)
        Bass.BASS_ChannelRemoveFX(_bassMixerStreamHandle, fx);
      _equalizer.Clear();
      EqualizerChanged?.Invoke(this, string.Empty);
    }
  } // RemoveEqualizer

  public void SetEqualizer(string name)
  {
    foreach (Equalizer eq in _equalizers) {
      if (string.Compare(eq.Name, name, true) == 0) {
        SetEqualizer(eq);
        break;
      }
    }
  }

  public void SetEqualizer(Equalizer eq)
  {
    if (eq == null)
      throw new ArgumentNullException("eq");
    if (eq.Bands == null || eq.Bands.Count != 10)
      throw new ArgumentException("Equalizer bands must be ten");

    if (_equalizer != null)
      RemoveEqualizer();

    int[] freq = new int[] { 32, 64, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };
    for (int idx=0; idx < 10; idx++) {
      EqualizerBand eqb = eq.Bands[idx];
      if (eqb.Gain > 15)
        eqb.Gain = 15;
      if (eqb.Gain < -15)
        eqb.Gain = -15;

      BASS_DX8_PARAMEQ eqp = new BASS_DX8_PARAMEQ();
      eqp.fBandwidth = 18.0F;
      eqp.fCenter = freq[idx];
      eqp.fGain = eqb.Gain;

      int fxHandle = Bass.BASS_ChannelSetFX(_bassMixerStreamHandle, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
      if (fxHandle != 0) {
        if (_equalizer == null)
          _equalizer = new List<int>();
        _equalizer.Add(fxHandle);
        Bass.BASS_FXSetParameters(fxHandle, eqp);
      }
    }
    _equalizerName = eq.Name;
    EqualizerChanged?.Invoke(this, eq.Name);
    AppSettings.Instance.EqualizerName = eq.Name;
    AppSettings.Instance.Save();
  } // SetEqualizer
}