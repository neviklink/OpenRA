#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Support;

namespace OpenRA.Widgets
{
	public class VqaPlayerWidget : Widget
	{
		Sprite videoSprite, overlaySprite;
		VqaReader video = null;
		string cachedVideo;
		float invLength;
		float2 videoOrigin, videoSize;
		int[,] overlay;
		public bool DrawOverlay = true;
		
		public void Load(string filename)
		{
			if (filename == cachedVideo)
				return;
			
			if (playing)
			{
				playing = false;
				Sound.StopVideoSoundtrack();
			}
			
			cachedVideo = filename;
			video = new VqaReader(FileSystem.Open(filename));
			invLength = video.Framerate*1f/video.Frames;

			var size = Math.Max(video.Width, video.Height);
			var textureSize = OpenRA.Graphics.Util.NextPowerOf2(size);
			videoSprite = new Sprite(new Sheet(new Size(textureSize,textureSize)), new Rectangle( 0, 0, video.Width, video.Height ), TextureChannel.Alpha);
			videoSprite.sheet.Texture.SetData(video.FrameData);

			var scale = Math.Min(RenderBounds.Width / video.Width, RenderBounds.Height / video.Height);
			videoOrigin = new float2(RenderBounds.X + (RenderBounds.Width - scale*video.Width)/2, RenderBounds.Y +  (RenderBounds.Height - scale*video.Height)/2);
			videoSize = new float2(video.Width * scale, video.Height * scale);
			
			if (!DrawOverlay)
				return;

			overlay = new int[2*textureSize, 2*textureSize];
			var black = Color.Black.ToArgb();
			for (var y = 0; y < video.Height; y++)
				for (var x = 0; x < video.Width; x++)
				overlay[2*y,x] = black;
			
			overlaySprite = new Sprite(new Sheet(new Size(2*textureSize,2*textureSize)), new Rectangle( 0, 0, video.Width, 2*video.Height ), TextureChannel.Alpha);
			overlaySprite.sheet.Texture.SetData(overlay);
		}
		
		bool playing = false;	
		Stopwatch sw = new Stopwatch();
		public override void DrawInner(World world)
		{
			if (video == null)
				return;
			
			if (playing)
			{
				var nextFrame = (int)float2.Lerp(0, video.Frames, (float)(sw.ElapsedTime()*invLength));
				if (nextFrame > video.Frames)
				{
					Stop();
					return;
				}
				
				while (nextFrame > video.CurrentFrame)
				{
					video.AdvanceFrame();
					if (nextFrame == video.CurrentFrame)
						videoSprite.sheet.Texture.SetData(video.FrameData);
				}
			}
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(videoSprite, videoOrigin, "chrome", videoSize);
			
			if (DrawOverlay)
				Game.Renderer.RgbaSpriteRenderer.DrawSprite(overlaySprite, videoOrigin, "chrome", videoSize);
		}
		
		public void Play()
		{
			if (playing || video == null)
				return;
			
			playing = true;
			sw.Reset();
			Sound.PlayVideoSoundtrack(video.AudioData);
		}
		
		public void Stop()
		{
			if (!playing || video == null)
				return;
			
			playing = false;
			Sound.StopVideoSoundtrack();
			video.Reset();
			videoSprite.sheet.Texture.SetData(video.FrameData);
		}
	}
}
