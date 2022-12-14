using UnityEngine;
using System.Collections.Generic;
using TutorialUnityMidi.RtMidiWrapper;

namespace TutorialUnityMidi
{
    public sealed class MidiInOutTest : MonoBehaviour
    {
        // Midi 信号を Out するポート指定用
        [SerializeField] private int outPort;
        
        // Midi In Out を使うための変数
        private MidiProbe _inProbe;
        private MidiProbe _outProbe;
        private readonly List<MidiInPort> _inPorts = new ();
        private readonly List<MidiOutPort> _outPorts = new ();

        // Unity で実行したとき、Start より前に Midi が使えるように変数に格納しておく
        private void Awake()
        {
            _inProbe = new MidiProbe(MidiProbe.Mode.In);
            _outProbe = new MidiProbe(MidiProbe.Mode.Out);
        }
        
        // 接続されている Midi デバイスをすべて取得し、Out ポートのサウンドを全て Off にする
        private void Start()
        {
            DisposePorts();
            ScanPorts();
            
            // Send an all-sound-off message.
            foreach (var port in _outPorts) port?.SendAllOff(0);
        }

        // Midi ポートに変化があれば、デバイスの取得を再度行う。
        // Midi In ポートにたまっている処理を行う。
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

        // Unity を終えたときに、Midi を使用するために生成したインスタンスを破棄する
        private void OnDestroy()
        {
            _inProbe?.Dispose();
            _outProbe?.Dispose();
            DisposePorts();
        }
        
        // そのポートが実在するかどうかを判定する
        private bool IsRealPort(string nm) => !nm.Contains("Through") && !nm.Contains("RtMidi");
        
        // 接続されている Midi デバイスをすべて取得する
        private void ScanPorts()
        {
            for (var i = 0; i < _outProbe.PortCount; i++)
            {
                var nm = _outProbe.GetPortName(i);
                // Out ポートの番号とデバイス名を出力
                Debug.Log($"MIDI-out No.{i} port found: " + nm);
                _outPorts.Add(IsRealPort(nm) ? new MidiOutPort(i) : null);
            }
            
            for (var i = 0; i < _inProbe.PortCount; i++)
            {
                var nm = _inProbe.GetPortName(i);
                // In ポートの番号とデバイス名を出力
                Debug.Log($"MIDI-in No.{i} port found: " + nm);

                _inPorts.Add(IsRealPort(nm) ? new MidiInPort(i)
                    {
                        OnNoteOn = (channel, note, velocity) =>
                        {
                            Debug.Log($"{nm} [{channel}] On {note} ({velocity})");
                            // Out ポートに NoteOn 信号を送る
                            _outPorts[outPort]?.SendNoteOn(channel, note, velocity);
                        },

                        OnNoteOff = (channel, note) =>
                        {
                            Debug.Log($"{nm} [{channel}] Off {note})");
                            // Out ポートに NoteOff 信号を送る
                            _outPorts[outPort]?.SendNoteOff(channel, note);
                        },

                        OnControlChange = (channel, number, value) =>
                            Debug.Log($"{nm} [{channel}] CC {number} ({value})")
                    } : null
                );
            }
        }

        // 取得していたデバイスを全て破棄する
        private void DisposePorts()
        {
            foreach (var p in _inPorts) p?.Dispose();
            foreach (var p in _outPorts) p?.Dispose();
            
            _inPorts.Clear();
            _outPorts.Clear();
        }
    }
}
