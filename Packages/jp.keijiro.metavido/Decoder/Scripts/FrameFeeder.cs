using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metavido.Decoder {

[AddComponentMenu("Metavido/Decoding/Metavido Frame Feeder")]
public sealed class FrameFeeder : IDisposable
{
    #region Private members

    (MetadataDecoder decoder, TextureDemuxer demuxer) _target;

    Queue<(RenderTexture rt, int index)> _queue
      = new Queue<(RenderTexture rt, int index)>();

    int _count;

    #endregion

    #region Public methods

    public FrameFeeder(MetadataDecoder decoder, TextureDemuxer demuxer)
      => _target = (decoder, demuxer);

    public void Dispose()
    {
        while (_queue.Count > 0)
            RenderTexture.ReleaseTemporary(_queue.Dequeue().rt);
    }

    public void AddFrame(Texture source)
    {
        // sRGB/Linear switch based on the source texture format
        var rw = source.isDataSRGB ? RenderTextureReadWrite.sRGB :
                                     RenderTextureReadWrite.Linear;

        // Source texture copy into a temporary RT
        var tempRT = RenderTexture.GetTemporary
          (source.width, source.height, 0, RenderTextureFormat.Default, rw);
        Graphics.CopyTexture(source, tempRT);

        // Decode queuing
        _target.decoder.RequestDecodeAsync(tempRT);
        _queue.Enqueue((tempRT, _count++));
    }

    public void Update()
    {
        // Decoder progress check
        if (_target.decoder.DecodeCount <= _queue.Peek().index) return;

        // Skipped frame disposal
        while (_queue.Peek().index < _target.decoder.DecodeCount - 1)
            RenderTexture.ReleaseTemporary(_queue.Dequeue().rt);

        // Demuxing with latest decoded frame
        var decoded = _queue.Dequeue().rt;
        _target.demuxer.Demux(decoded, _target.decoder.Metadata);
        RenderTexture.ReleaseTemporary(decoded);
    }

    #endregion
}

} // namespace Metavido.Decoder
