using UnityEngine;
using System.Collections.Generic;
using TutorialUnityMidi.RtMidiWrapper;

namespace TutorialUnityMidi
{
    public sealed class MidiInOutTest : MonoBehaviour
    {
        [SerializeField] private int outPort;
        
        private MidiProbe _inProbe;
        private MidiProbe _outProbe;
        private readonly List<MidiInPort> _inPorts = new ();
        private readonly List<MidiOutPort> _outPorts = new ();

        private void Awake()
        {
            _inProbe = new MidiProbe(MidiProbe.Mode.In);
            _outProbe = new MidiProbe(MidiProbe.Mode.Out);
        }
        
        private void Start()
        {
            // Send an all-sound-off message.
            foreach (var port in _outPorts) port?.SendAllOff(0);
        }

        private void Update()
        {
            // Rescan when the number of ports changed.
            if (_inPorts.Count != _inProbe.PortCount)
            {
                DisposePorts();
                ScanPorts();
            }

            // Process queued messages in the opened ports.
            foreach (var p in _inPorts) p?.ProcessMessages();
        }

        private void OnDestroy()
        {
            _inProbe?.Dispose();
            DisposePorts();
        }
        
        // Does the port seem real or not?
        // This is mainly used on Linux (ALSA) to filter automatically generated
        // virtual ports.
        private bool IsRealPort(string nm) => !nm.Contains("Through") && !nm.Contains("RtMidi");
        
        // Scan and open all the available output ports.
        private void ScanPorts()
        {
            for (var i = 0; i < _outProbe.PortCount; i++)
            {
                var nm = _outProbe.GetPortName(i);
                Debug.Log("MIDI-out port found: " + nm);
                _outPorts.Add(IsRealPort(nm) ? new MidiOutPort(i) : null);
            }
            
            for (var i = 0; i < _inProbe.PortCount; i++)
            {
                var nm = _inProbe.GetPortName(i);
                Debug.Log("MIDI-in port found: " + nm);

                _inPorts.Add(IsRealPort(nm) ? new MidiInPort(i)
                    {
                        OnNoteOn = (channel, note, velocity) =>
                        {
                            Debug.Log($"{nm} [{channel}] On {note} ({velocity})");
                            _outPorts[outPort]?.SendNoteOn(channel, note, velocity);
                        },

                        OnNoteOff = (channel, note) =>
                        {
                            Debug.Log($"{nm} [{channel}] Off {note})");
                            _outPorts[outPort]?.SendNoteOff(channel, note);
                        },

                        OnControlChange = (channel, number, value) =>
                            Debug.Log($"{nm} [{channel}] CC {number} ({value})")
                    } : null
                );
            }
        }

        // Close and release all the opened ports.
        private void DisposePorts()
        {
            foreach (var p in _inPorts) p?.Dispose();
            foreach (var p in _outPorts) p?.Dispose();
            
            _inPorts.Clear();
            _outPorts.Clear();
        }
    }
}
