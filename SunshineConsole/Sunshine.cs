/*Copyright (c) 2015  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace SunshineConsole{
	/*public static class SunshineMain{ // Here's a quick example.
		public static void Main(){
			ConsoleWindow console = new ConsoleWindow(20,100,"Sunshine Console: The Roguelike");
			int row = 10; // These 2 ints are the player's position.
			int col = 40;
			while(console.WindowUpdate()){ // WindowUpdate() returns false if the window is closed, so be sure to check for that.
				for(int i=0;i<20;++i){
					console.Write(i,0,"".PadRight(100,'#'),Color4.DimGray); // Let's make our black screen look more like a dungeon.
				}
				console.Write(row,col,'@',Color4.White); // And of course, our player character.
				if(console.KeyPressed){ // KeyPressed returns true if there's a new key to grab.
					switch(console.GetKey()){ // If KeyPressed is false, GetKey() will return Key.Unknown.
					case Key.Up:
					row = Math.Max(0,row-1); // In our basic example, we only check for arrow keys.
					break;
					case Key.Down:
					row = Math.Min(row+1,19); // We make sure that row & col don't go beyond the edges of the map.
					break;
					case Key.Left:
					col = Math.Max(0,col-1);
					break;
					case Key.Right:
					col = Math.Min(col+1,99);
					break;
					}
				}
				System.Threading.Thread.Sleep(10); // A call to Sleep() will prevent our program from using 100% CPU all the time.
			} // And that's all you really need to get up and running!
		}
	}*/
	public class ConsoleWindow : GameWindow{
		protected char[,] chars;
		protected Color4[,] colors;
		protected Color4[,] bgcolors;
		protected int internal_rows;
		protected int internal_cols;
		protected int first_changed_row,last_changed_row,first_changed_col,last_changed_col;
		protected bool hold_updates;
		protected bool internal_key_pressed;
		protected Key internal_last_key;
		protected FrameEventArgs render_args = new FrameEventArgs(); //This is a necessary step if you're not using the default GameWindow loop.
		protected int num_elements;
		protected static float half_height;
		protected static float half_width;
		protected const int font_w = 8;
		protected const int font_h = 16;
		protected const float font_texcoord_width = 1.0f / 128.0f;
		protected static readonly float font_texcoord_padding = font_texcoord_width * 8.0f / 9.0f;
		protected const string font_filename = "font8x16.bmp";
		public ConsoleWindow(int rows,int columns,string window_title) : base(columns*font_w,rows*font_h,GraphicsMode.Default,window_title){
			VSync = VSyncMode.On;
			GL.ClearColor(0.0f,0.0f,0.0f,0.0f);
			chars = new char[rows,columns];
			colors = new Color4[rows,columns];
			bgcolors = new Color4[rows,columns];
			internal_rows = rows;
			internal_cols = columns;
			half_height = (float)rows * 0.5f;
			half_width = (float)columns * 0.5f;
			ResetChangedPositions();
			KeyDown += (sender,e) => {
				if(!internal_key_pressed){
					internal_key_pressed = true;
					internal_last_key = e.Key;
				}
			};
			LoadTexture(font_filename);
			LoadShaders();
			CreateVBO(rows,columns);
			Visible = true;
			Resize += (sender,e) => {
				Height = rows * font_h;
				Width = columns * font_w;
			};
			//WindowBorder = WindowBorder.Fixed;
		}
		public char GetChar(int row,int col){ return chars[row,col]; }
		public Color4 GetColor(int row,int col){ return colors[row,col]; }
		public Color4 GetBackgroundColor(int row,int col){ return bgcolors[row,col]; }
		public int Rows{ get{ return internal_rows; } }
		public int Cols{ get{ return internal_cols; } }
		public bool KeyPressed{ get{ return internal_key_pressed; } }
		public Key GetKey(){
			if(internal_key_pressed){
				internal_key_pressed = false;
				return internal_last_key;
			}
			return Key.Unknown;
		}
		public bool KeyIsDown(Key key){ return OpenTK.Input.Keyboard.GetState().IsKeyDown(key); }
		public void Write(int row,int col,char ch,Color4 color){ Write(row,col,"" + ch,color,Color4.Black); }
		public void Write(int row,int col,char ch,Color4 color,Color4 bgcolor){ Write(row,col,"" + ch,color,bgcolor); }
		public void Write(int row,int col,string s,Color4 color){ Write(row,col,s,color,Color4.Black); }
		public void Write(int row,int col,string s,Color4 color,Color4 bgcolor){
			int i = 0;
			foreach(char ch in s){
				if(InBounds(row,col+i)){
					bool changed = false;
					if(chars[row,col+i] != ch){
						chars[row,col+i] = ch;
						changed = true;
					}
					if(colors[row,col+i] != color){
						colors[row,col+i] = color;
						changed = true;
					}
					if(bgcolors[row,col+i] != bgcolor){
						bgcolors[row,col+i] = bgcolor;
						changed = true;
					}
					if(changed){
						if(row < first_changed_row){
							first_changed_row = row;
						}
						if(row > last_changed_row){
							last_changed_row = row;
						}
						if(col+i < first_changed_col){
							first_changed_col = col+i;
						}
						if(col+i > last_changed_col){
							last_changed_col = col+i;
						}
					}
				}
				++i;
			}
			UpdateGLBuffer();
		}
		protected bool InBounds(int row,int col){
			return !(row < 0 || col < 0 || row >= internal_rows || col >= internal_cols);
		}
		protected void UpdateGLBuffer(){
			if(!hold_updates && last_changed_row > -1){ //Any change will set all 4 values to a valid position, so checking any of them will suffice.
				int num_positions = ((last_changed_col + last_changed_row*internal_cols) - (first_changed_col + first_changed_row*internal_cols)) + 1;
				List<float> values = new List<float>(48 * num_positions);
				int row = first_changed_row;
				int col = first_changed_col;
				while(true){
					Color4 color = colors[row,col];
					Color4 bgcolor = bgcolors[row,col];
					float tex_start = font_texcoord_width * (int)chars[row,col];
					float tex_end = tex_start + font_texcoord_padding;
					int flipped_row = internal_rows - 1 - row;
					float fi = ((float)flipped_row / half_height) - 1.0f;
					float fj = ((float)col / half_width) - 1.0f;
					float fi_plus1 = ((float)(flipped_row+1) / half_height) - 1.0f;
					float fj_plus1 = ((float)(col+1) / half_width) - 1.0f;
					values.AddRange(new float[]{
						fj,fi,tex_start,1,color.R,color.G,color.B,color.A,bgcolor.R,bgcolor.G,bgcolor.B,bgcolor.A,
						fj,fi_plus1,tex_start,0,color.R,color.G,color.B,color.A,bgcolor.R,bgcolor.G,bgcolor.B,bgcolor.A,
						fj_plus1,fi_plus1,tex_end,0,color.R,color.G,color.B,color.A,bgcolor.R,bgcolor.G,bgcolor.B,bgcolor.A,
						fj_plus1,fi,tex_end,1,color.R,color.G,color.B,color.A,bgcolor.R,bgcolor.G,bgcolor.B,bgcolor.A});
					if(col == last_changed_col && row == last_changed_row){
						break;
					}
					col++;
					if(col == internal_cols){
						row++;
						col = 0;
					}
				}
				int idx = (first_changed_col + first_changed_row*internal_cols) * 48;
				GL.BufferSubData(BufferTarget.ArrayBuffer,new IntPtr(sizeof(float)*idx),new IntPtr(sizeof(float)*48*num_positions),values.ToArray());
				ResetChangedPositions();
			}
		}
		protected void ResetChangedPositions(){
			first_changed_row = internal_rows; //these 4 are set to out of bounds values.
			first_changed_col = internal_cols;
			last_changed_row = -1;
			last_changed_col = -1;
		}
		public void HoldUpdates(){
			hold_updates = true;
		}
		public void ResumeUpdates(){
			hold_updates = false;
			UpdateGLBuffer();
		}
		public bool WindowUpdate(){
			ProcessEvents();
			if(IsExiting){
				return false;
			}
			Render();
			return true;
		}
		protected void Render(){
			base.OnRenderFrame(render_args);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			GL.DrawElements(PrimitiveType.Triangles,num_elements,DrawElementsType.UnsignedInt,IntPtr.Zero);
			SwapBuffers();
		}
		protected void LoadTexture(string filename){
			if(String.IsNullOrEmpty(filename)){
				throw new ArgumentException(filename);
			}
			int id = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D,id);
			Bitmap bmp = new Bitmap(filename);
			BitmapData bmp_data = bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D(TextureTarget.Texture2D,0,PixelInternalFormat.Rgba,bmp_data.Width,bmp_data.Height,0,OpenTK.Graphics.OpenGL.PixelFormat.Bgra,PixelType.UnsignedByte,bmp_data.Scan0);
			bmp.UnlockBits(bmp_data);
			GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMagFilter.Nearest);
		}
		protected void LoadShaders(){
			int vertex_shader = GL.CreateShader(ShaderType.VertexShader);
			int fragment_shader = GL.CreateShader(ShaderType.FragmentShader);
			GL.ShaderSource(vertex_shader,
				@"#version 120
attribute vec4 position;
attribute vec2 texcoord;
attribute vec4 color;
attribute vec4 bgcolor;

varying vec2 texcoord_fs;
varying vec4 color_fs;
varying vec4 bgcolor_fs;

void main(){
texcoord_fs = texcoord;
color_fs = color;
bgcolor_fs = bgcolor;
gl_Position = position;
}
");
			GL.ShaderSource(fragment_shader,
				@"#version 120
uniform sampler2D texture;

varying vec2 texcoord_fs;
varying vec4 color_fs;
varying vec4 bgcolor_fs;

void main(){
vec4 v = texture2D(texture,texcoord_fs);
if(v.r == 1.0 && v.g == 1.0 && v.b == 1.0){
gl_FragColor = color_fs;
}
else{
gl_FragColor = bgcolor_fs;
}
}
");
			GL.CompileShader(vertex_shader);
			GL.CompileShader(fragment_shader);
			int compiled;
			GL.GetShader(vertex_shader,ShaderParameter.CompileStatus,out compiled);
			if(compiled < 1){
				Console.Error.WriteLine(GL.GetShaderInfoLog(vertex_shader));
				throw new Exception("vertex shader compilation failed");
			}
			GL.GetShader(fragment_shader,ShaderParameter.CompileStatus,out compiled);
			if(compiled < 1){ 
				Console.Error.WriteLine(GL.GetShaderInfoLog(fragment_shader));
				throw new Exception("fragment shader compilation failed");
			}
			int shader_program = GL.CreateProgram();
			GL.AttachShader(shader_program,vertex_shader);
			GL.AttachShader(shader_program,fragment_shader);
			GL.BindAttribLocation(shader_program,0,"position");
			GL.BindAttribLocation(shader_program,1,"texcoord");
			GL.BindAttribLocation(shader_program,2,"color");
			GL.BindAttribLocation(shader_program,3,"bgcolor");
			GL.LinkProgram(shader_program);
			GL.UseProgram(shader_program);
		}
		protected void CreateVBO(int rows,int cols){
			float[] f = new float[rows * cols * 48]; //4 vertices, 12 pieces of data.
			num_elements = rows * cols * 6;
			int[] indices = new int[num_elements];
			for(int i=0;i<rows;++i){
				for(int j=0;j<cols;++j){
					int idx = (j + i*cols) * 48;
					int flipped_row = (rows-1) - i;
					float fi = ((float)flipped_row / half_height) - 1.0f;
					float fj = ((float)j / half_width) - 1.0f;
					float fi_plus1 = ((float)(flipped_row+1) / half_height) - 1.0f;
					float fj_plus1 = ((float)(j+1) / half_width) - 1.0f;
					float[] values = new float[]{fj,fi,0,1,1,1,1,1,0,0,0,0,  fj,fi_plus1,0,0,1,1,1,1,0,0,0,0,  fj_plus1,fi_plus1,font_texcoord_width,0,1,1,1,1,0,0,0,0,  fj_plus1,fi,font_texcoord_width,1,1,1,1,1,0,0,0,0};
					values.CopyTo(f,idx); //x, y, s, t, r, g, b, a, bgr, bgg, bgb, bga

					int idx4 = (j + i*cols) * 4;
					int idx6 = (j + i*cols) * 6;
					indices[idx6] = idx4;
					indices[idx6 + 1] = idx4 + 1;
					indices[idx6 + 2] = idx4 + 2;
					indices[idx6 + 3] = idx4;
					indices[idx6 + 4] = idx4 + 2;
					indices[idx6 + 5] = idx4 + 3;
				}
			}
			int vert_id;
			int elem_id;
			GL.GenBuffers(1,out vert_id);
			GL.GenBuffers(1,out elem_id);
			GL.BindBuffer(BufferTarget.ArrayBuffer,vert_id);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer,elem_id);
			GL.BufferData(BufferTarget.ArrayBuffer,new IntPtr(sizeof(float)*f.Length),f,BufferUsageHint.StreamDraw);
			GL.BufferData(BufferTarget.ElementArrayBuffer,new IntPtr(sizeof(int)*indices.Length),indices,BufferUsageHint.StaticDraw);
			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.EnableVertexAttribArray(2);
			GL.EnableVertexAttribArray(3);
			GL.VertexAttribPointer(0,2,VertexAttribPointerType.Float,false,sizeof(float)*12,0);
			GL.VertexAttribPointer(1,2,VertexAttribPointerType.Float,false,sizeof(float)*12,new IntPtr(sizeof(float)*2));
			GL.VertexAttribPointer(2,4,VertexAttribPointerType.Float,false,sizeof(float)*12,new IntPtr(sizeof(float)*4));
			GL.VertexAttribPointer(3,4,VertexAttribPointerType.Float,false,sizeof(float)*12,new IntPtr(sizeof(float)*8));
		}
	}
}
