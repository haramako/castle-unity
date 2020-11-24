using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioSampler : MonoBehaviour
{
	const int SamplerRate = 48000;
	const int BufLength = 800 * 120;

	AudioSource source_;
	AudioClip clip_;

	int position_;

	UInt16[] buf_;
	int bufHead_;
	int bufTail_;
	int bufTotal_;
	int readTotal_;

	// Start is called before the first frame update
	void Start()
	{
		buf_ = new UInt16[BufLength];
		bufHead_ = 0;
		bufTail_ = 0;

		source_ = GetComponent<AudioSource>();
		source_.loop = true;
		StartClip();
	}

	public void PCMSetPositionCallback(int position)
	{
		//Debug.Log($"new position {position}");
		position_ = position;
	}

	void StartClip()
	{
		clip_ = AudioClip.Create("AudioSampler", 44100, 1, SamplerRate, false);
		source_.time = 0;
		source_.clip = clip_;
		source_.Play();
	}

	public unsafe void Fill(UInt16* samples, int frame)
	{
		fillCount_++;
		int len = frame * 2;
		for (int i = 0; i < len; i++)
		{
			buf_[bufTail_] = samples[i];
			bufTail_ = (bufTail_ + 1) % BufLength;
		}
		bufTotal_ += len;
	}

	void OnAudioFilterRead(float[] data, int channels)
	{
		filterReadCount_++;
		//Debug.Log(data.Length);
		int dataLen = data.Length / channels;

		if (bufTotal_ - readTotal_ > 4096 * 2)
		{
			readTotal_ += data.Length * 1;
			Debug.Log("buf over");
		}

		if (bufTotal_ - readTotal_ < data.Length)
		{
			Debug.Log("buf less");
			return;
		}

		//Debug.Log($"data readTotal {readTotal_}, total {bufTotal_}, rest {bufTotal_- bufPos2_}");

		for (int i = 0; i < dataLen; i++)
		{
			var bufPos_ = readTotal_ + i * 2; // (int)((readTotal_ + i) / rate * SamplerRate );
			bufPos_ = bufPos_ % BufLength;

			for (int ch = 0; ch < channels; ch++)
			{
				data[i * channels + ch] += ((int)(Int16)buf_[bufPos_ + ch]) / 36768f;
			}
			//bufHead_ = (bufHead_ + 1) % BufLength;
		}

		readTotal_ += dataLen * channels;
	}

	int filterReadCount_;
	int fillCount_;
	int lastUpdated_ = 1;
	int prevRead_;
	int prevBuf_;
	private void Update()
	{
		if ( Time.time > lastUpdated_)
		{
			if (lastUpdated_ % 5 == 0)
			{
				Debug.Log($"Audio buffer state sampleRate {AudioSettings.outputSampleRate}, fill {fillCount_}, readCount {filterReadCount_}, read {readTotal_ - prevRead_}, buf {bufTotal_ - prevBuf_}, rest {bufTotal_ - readTotal_}");
			}
			filterReadCount_ = 0;
			fillCount_ = 0;
			prevRead_ = readTotal_;
			prevBuf_ = bufTotal_;
			lastUpdated_ += 1;
		}

	}
}


