using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Un4seen.Bass;

namespace CAuLi
{
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
    string m_EqualizerName = string.Empty;
    List<int> m_Equalizer = null;
    List<Equalizer> m_Equalizers = new List<Equalizer>();

    public delegate void EqualizerChangedHandler(Player sender, string eqName);
    public EqualizerChangedHandler EqualizerChanged;

    public bool EqualizerEnabled
    {
      get
      {
        return m_Equalizer != null && m_Equalizer.Count > 0;
      }
    }

    public string EqualizerName
    {
      get { return m_EqualizerName; }
    }

    public List<Equalizer> Equalizers
    {
      get { return m_Equalizers; }
    }

    public void LoadStandardEqualizers()
    {
      string eqPath = Path.Combine(Program.RootPath, "Equalizers");
      if (Directory.Exists(eqPath)) {
        foreach (string f in Directory.GetFiles(eqPath, "*.xml")) {
          using (StreamReader sr = new StreamReader(f)) {
            Equalizer eq = Utility.Serialization.Deserialize<Equalizer>(sr.BaseStream);
            if (eq != null)
              m_Equalizers.Add(eq);
          }
        }
      }
    } // LoadStandardEqualizers

    public void RemoveEqualizer()
    {
      if (m_Equalizer != null) {
        foreach (int fx in m_Equalizer)
          Bass.BASS_ChannelRemoveFX(m_BassMixerStreamHandle, fx);
        m_Equalizer.Clear();
        EqualizerChanged?.Invoke(this, string.Empty);
      }
    } // RemoveEqualizer

    public void SetEqualizer(string name)
    {
      foreach (Equalizer eq in m_Equalizers) {
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

      if (m_Equalizer != null)
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

        int fxHandle = Bass.BASS_ChannelSetFX(m_BassMixerStreamHandle, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
        if (fxHandle != 0) {
          if (m_Equalizer == null)
            m_Equalizer = new List<int>();
          m_Equalizer.Add(fxHandle);
          Bass.BASS_FXSetParameters(fxHandle, eqp);
        }        
      }
      m_EqualizerName = eq.Name;
      EqualizerChanged?.Invoke(this, eq.Name);
      AppSettings.Instance.EqualizerName = eq.Name;
      AppSettings.Instance.Save();
    } // SetEqualizer
  }
}
