# SunshineConsole
A C# library for simple ASCII output/input

Sunshine Console is intended to be very friendly to beginners that just want to put something on the screen (while still being powerful enough for larger ASCII projects). So, here are the very basics:

First, you'll need to add Sunshine Console to your project. Instructions for that are [here](http://derrickcreamer.github.io/SunshineConsole/).
```c#
// I recommend adding these 3 'using' directives for easier access to ConsoleWindow, Color4, and Key, respectively:
using SunshineConsole;
using OpenTK.Graphics;
using OpenTK.Input;

// Create a window, 16 rows high and 40 columns across:
ConsoleWindow console = new ConsoleWindow(16,40,"Sunshine Console Hello World");

// Write to the window at row 6, column 14:
console.Write(6,14,"Hello World!",Color4.Lime);

// Finally, update the window until a key is pressed or the window is closed:
while(!console.KeyPressed && console.WindowUpdate()){
  /* WindowUpdate() does a few very important things:
  ** It renders the console to the screen;
  ** It checks for input events from the OS, such as keypresses, so that they can reach the program;
  ** It returns false if the console has been closed, so that the program can be properly ended. */
}
```

That's all you need to get off the ground!

&nbsp;

#### Other features

##### Checking what's onscreen:
```c#
// Retrieve all the information about row 0, column 4:
char ch = console.GetChar(0,4);
Color4 color = console.GetColor(0,4);
Color4 bgcolor = console.GetBackgroundColor(0,4);
```

##### Checking for new keypresses:
```c#
if(console.KeyPressed){
  Key key = console.GetKey();
  // ...
}
```
##### Checking whether a key is currently being held down:
```c#
// Check whether either Control key is currently being held:
if(console.KeyIsDown(Key.LControl) || console.KeyIsDown(Key.RControl)){
	// ...
}
```

#### Advanced features
##### Holding and resuming updates:
Here's a feature you probably won't need. If you know that you're planning to write a lot to the console and you don't want each change to be immediately pushed to the OpenGL buffer (note that pushing to the OpenGL buffer doesn't automatically push it to the screen!), you can use HoldUpdates() and ResumeUpdates():
```c#
console.HoldUpdates();
console.Write(5,5,'#',Color4.Blue);
// ... Here, let's just pretend that there are a lot more than 2 writes.
console.Write(8,5,'^',Color4.White);
// At this point, your changes have been noted, but they won't show up on the screen yet!
console.ResumeUpdates();
// Now that you've called ResumeUpdates(), all the changes you made have been sent to the OpenGL buffer.
// Those changes will be visible next time you call WindowUpdate()!
```

##### Other OpenTK features:
OpenTK has a lot of great features that you can access, because ConsoleWindow inherits from OpenTK.GameWindow. Among these are event handlers for window closing, focus changing, and even mouse input! Learn more at the [GameWindow reference](http://www.opentk.com/files/doc/class_open_t_k_1_1_game_window.html) and the [OpenTK docs](http://www.opentk.com/doc).
