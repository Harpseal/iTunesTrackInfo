# iTunesTrackInfo

A sample iTunes tool to show track information and control iTunes by global hotkey.

## Screenshot

| Rainmeter (iTunes Player 2 skin)   |
|:-------------:|
| ![Rainmeter](http://lh5.ggpht.com/-FeN6iUQKZxI/Ur5NoAiGAGI/AAAAAAAABkA/OfZfA9KgFJg/s640/iTunesTrackInfo_rainmeter.png) |

| iTunesTrackInfo |
|:---------------:|
| ![iTunesTrackInfo](http://lh4.ggpht.com/-oW7Pvbo20PA/Ur5Nm8Z6mxI/AAAAAAAABj4/FVm4JJhXEdM/s640/iTunesTrackInfo.png)|

### Lyrics (*.lrc, *.ass and *.srt are supported.)
![Screenshot_lyrics](http://lh4.ggpht.com/-_C7vDrN9r34/UxqR1ER5W9I/AAAAAAAABlg/tvRiS_jNwZE/s640/Screenshot_lyrics.png)

 
## The reason I gave up Rainmeter which have been used for several years

There are two things that bother me when I use Rainmeter to show current iTunes information. 

1. The NowPlaying plugin of Rainmeter 3 isn't support the *half-star* rating which I use very often to manage my music database.
2. The artwork image IO function of the old iTunesPlugin of Rainmeter 2 isn't optimizated which save a *new* artwork image file to disk at each UI update event even if the player is stopped.

Due to the above two reasons, I spent one day to rebuild a tool which has the same UI of "iTunes Player 2" skin and add the global hotkey rating function to replace another autohotkey script.


## Rainmeter iTunes skins/iTunesTrackInfo compare

|                     |   Rainmeter |        iTunesTrackInfo            |
|:-------------------:|:-----------:|:---------------------------------:|
|Custom global hotkey |      X      |   ? (modify&build code)           |
|Custom skins         |      V      |   ? (WPF UI editor + build code) |
|Hide on mouse over + click through  |      V      |   V                |


## C#/WPF programming examples

* How to control iTunes in C# (including get/set track info, player control, iTunes event callback)
* C# WPF transparent window
* Creating animation using C# (fade-in&fade-out)
* Hide on mouse over + click through
* [Global hotkey hook](http://www.codeproject.com/Articles/7294/Processing-Global-Mouse-and-Keyboard-Hooks-in-C).
* Single instance check by using creating mutex (not using wait/release mutex) 
* Multi-Thread event synchronization


## Todo
```
Commenting Code
```

## Buliding Prerequisites & Dependencies
- Microsoft Visual Studio 2012/2013 (Express version is enough)


## Attribution

* All icons used in this project are made by android team and downloaded from [deviantart](http://palhaiz.deviantart.com/art/Android-4-1-Jelly-Bean-Icon-Set-311741892)
* The C# global hotkey hook class "UserActivityHook" is borrow from [George Mamaladze](http://www.codeproject.com/Articles/7294/Processing-Global-Mouse-and-Keyboard-Hooks-in-C).

Thanks for their hard work. :)
