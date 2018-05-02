\ MINOS2 widget basis

\ Copyright (C) 2014,2016,2017 Free Software Foundation, Inc.

\ This file is part of Gforth.

\ Gforth is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public License
\ as published by the Free Software Foundation, either version 3
\ of the License, or (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with this program. If not, see http://www.gnu.org/licenses/.

\ A MINOS2 widget is composed of drawable elements, boxes and actors.
\ to make things easier, neither drawable elements nor boxes need an actor.

debug: time(
\ +db time( \ )

[IFUNDEF] no-file#
    2 Constant ENOENT
    #-512 ENOENT - Constant no-file#
[THEN]

require i18n.fs \ localization
require gl-terminal.fs

ctx 0= [IF] window-init [THEN]
require ftgl-helper.fs
require mini-oof2.fs
require config.fs

get-current
also [IFDEF] android android [THEN]
also opengl

: #Variable ( init -- )  Create , ;

vocabulary minos  also minos definitions

0 Value x-color
: color: ( rgba "name" -- )
    Create , DOES> @ to x-color ;
: color, ( rgba -- [rgba] ) \ pseudonymous color
;

vocabulary m2c \ minos2 config
get-current also m2c definitions

$000000FF #Variable cursorcolor#
$3F7FFF7F #Variable selectioncolor#
Variable curminchars#
FVariable curminwidth%
FVariable pwtime%
set-current

0 curminchars# !
1e curminwidth% f!
0.5e pwtime% f!

previous

Variable configured?
Variable config-file$  s" ~/.minos2rc" config-file$ $!

: ?.minos-config ( -- )  true configured? !@ ?EXIT
    s" MINOS2_CONF" getenv dup IF  config-file$ $!  ELSE  2drop  THEN
    config-file$ $@ 2dup file-status nip ['] m2c >body swap
    no-file# = IF  write-config  ELSE  read-config  THEN ;

?.minos-config

$01 Constant box-hflip#
$02 Constant box-vflip#
$03 Value box-flip#

object class
    value: caller-w
    value: active-w
    method clicked ( rx ry bmask n -- ) \ processed clicks
    method scrolled ( axis dir -- ) \ process scrolling
    method touchdown ( $rxy*n bmask -- ) \ raw click
    method touchup ( $rxy*n bmask -- ) \ raw click
    method touchmove ( $rxy*n bmask -- ) \ raw click, bmask=0 is hover
    method ukeyed ( addr u -- ) \ printable unicode characters
    method ekeyed ( ekey -- ) \ non-printable keys
    method inside? ( rx ry -- flag )
    method focus ( -- )
    method defocus ( -- )
    method show ( -- )
    method hide ( -- )
    method get ( -- something )
    method set ( something -- )
    method show-you ( -- )
end-class actor

object class
    method hglue!@
    method dglue!@
    method vglue!@
    method aidglue0 \ zero glues
    method aidglue=
end-class helper-glue

' noop helper-glue is hglue!@
' noop helper-glue is vglue!@
' noop helper-glue is dglue!@
' noop helper-glue is aidglue0
' true helper-glue is aidglue= \ if equal, no need to rerun

\ dummy methods for empty actor, used for inheritance
:noname 2drop fdrop fdrop ; actor is clicked
' 2drop actor is scrolled
' 2drop actor is touchdown
' 2drop actor is touchup
' 2drop actor is touchmove
' 2drop actor is ukeyed
' drop actor is ekeyed
:noname fdrop fdrop false ; actor is inside?
' noop actor is focus
' noop actor is defocus
' noop actor is show
' noop actor is hide
' noop actor is get
' noop actor is set
' noop actor is show-you

object class
    value: parent-w
    value: act
    sfvalue: x
    sfvalue: y
    sfvalue: w
    sfvalue: h \ above baseline
    sfvalue: d \ below baseline
    sfvalue: border    \ surrounding border, all directions
    sfvalue: borderv   \ vertical border offset
    sfvalue: bordert   \ top border offset
    sfvalue: borderl   \ left border offset
    sfvalue: kerning   \ add kerning
    sfvalue: raise     \ raise/lower box
    sfvalue: baseline  \ minimun skip per line
    method draw-init ( -- ) \ init draw
    method draw-bg ( -- ) \ button background draw
    method draw-image ( -- ) \ image draw
    method draw-text ( -- ) \ text draw
    method hglue ( -- rtyp rsub radd )
    method dglue ( -- rtyp rsub radd )
    method vglue ( -- rtyp rsub radd )
    method hglue@ ( -- rtyp rsub radd ) \ cached variant
    method dglue@ ( -- rtyp rsub radd ) \ cached variant
    method vglue@ ( -- rtyp rsub radd ) \ cached variant
    method xywh ( -- rx0 ry0 rw rh )
    method xywhd ( -- rx ry rw rh rd )
    method resize ( rx ry rw rh rd -- )
    method !size \ set your own size
end-class widget

:noname x y h f- w h d f+ ; widget is xywh
:noname x y w h d ; widget is xywhd
' noop widget is !size
:noname w border f2* f+ borderl f+ kerning f+ 0e fdup ; widget is hglue
:noname h border borderv f+ bordert f+ raise f+ f+ 0e fdup ; widget is vglue
:noname d border borderv f+ raise f- f+ 0e fdup ; widget is dglue
: widget-resize to d to h to w to y to x ;
' widget-resize widget is resize
' hglue widget is hglue@
' vglue widget is vglue@
' dglue widget is dglue@

: dw* ( f -- f' ) dpy-w @ fm* ;
: dh* ( f -- f' ) dpy-h @ fm* ;

\ glues

begin-structure glues
    dffield: glue-t \ typical size
    dffield: glue-s \ shrink by
    dffield: glue-a \ add by
end-structure

widget class
    glues +field hglue-c
    glues +field dglue-c
    glues +field vglue-c
end-class glue

: df@+ ( addr -- u addr' )  dup df@ dfloat+ ;
: df!- ( addr -- u addr' )  dup df! [ 1 dfloats ]L - ;
: glue@ ( addr -- t s a )  df@+ df@+ df@ ;
: glue! ( t s a addr -- )  [ 2 dfloats ]L + df!- df!- df! ;
:noname hglue-c glue@ ; dup glue is hglue@ glue is hglue
:noname dglue-c glue@ ; dup glue is dglue@ glue is dglue
:noname vglue-c glue@ ; dup glue is vglue@ glue is vglue

\ tile widget

widget class
    value: frame-color
    value: tile-glue \ glue object
    value: frame#
end-class tile

:noname tile-glue .hglue { f: s f: a } border f2* borderl f+ f+ s a ; tile is hglue
:noname tile-glue .dglue { f: s f: a } border borderv f+ f+ s a ; tile is dglue
:noname tile-glue .vglue { f: s f: a } border borderv f+ bordert f+ f+ s a ; tile is vglue

begin-structure atlas-region
    slvalue: i.x
    slvalue: i.y
    slvalue: i.w
    slvalue: i.h
end-structure

: #>st ( x y frame -- ) \ using frame#
    dup i.h fm* dup i.y s>f f+ fswap
    dup i.w fm*     i.x s>f f+ fswap >st ;

: draw-rectangle { f: x1 f: y1 f: x2 f: y2 -- }
    frame-color ?dup-IF
	-1e to t.i0
	frame# i>off >v
	x1 y1 >xy over rgba>c n> 0e 0e dup #>st v+
	x2 y1 >xy over rgba>c n> 1e 0e dup #>st v+
	x1 y2 >xy over rgba>c n> 0e 1e dup #>st v+
	x2 y2 >xy swap rgba>c n> 1e 1e     #>st v+
	v> 2 quad  #4 ?flush-tris
    THEN ;
: >xyxy ( rx ry rw rh -- rx0 ry0 rx1 ry1 )
    { f: w f: h } fover w f+ fover h f+ ;
: tile-draw ( -- )
    xywh >xyxy draw-rectangle ;

' tile-draw tile is draw-bg

\ tile that doesn't draw

tile class
end-class glue-tile

' noop tile is draw-bg

\ image widget

tile class
    defer: image-tex
end-class image

' noop       image is draw-bg
: xywh-rect ( color -- )
    xywh >xyxy { f: x1 f: y1 f: x2 f: y2 -- }
    i>off  >v
    x1 y1 >xy dup rgba>c n> 0e 0e >st v+
    x2 y1 >xy dup rgba>c n> 1e 0e >st v+
    x1 y2 >xy dup rgba>c n> 0e 1e >st v+
    x2 y2 >xy     rgba>c n> 1e 1e >st v+
    v> 2 quad
    GL_TRIANGLES draw-elements ;
:noname ( -- )
    z-bias set-color+ image-tex  frame-color vi0 xywh-rect ;
image is draw-image

\ frame widget

tile class
end-class frame

Create button-st  0e sf, 0.25e sf, 0.75e sf, 1e sf,
DOES>  swap sfloats + sf@ ;
: button-border ( n -- gray )  dup 2/ xor ;
: >border ( rx rb i rw -- r ) { f: w }
    button-border dup
    1 and 0= IF fdrop 0e      THEN
    2 and    IF fnegate w f+  THEN  f+ ;

: frame-draw ( -- )
    -1e to t.i0
    frame# frame-color border fdup borderv f+
    xywh { f c f: b f: bv f: x f: y f: w f: h }
    i>off >v
    4 0 DO
	4 0 DO
	    x b I w >border
	    y bv J h >border >xy
	    c rgba>c  n>
	    I button-st J button-st f #>st v+
	LOOP
    LOOP
    v>
    9 0  DO
	4 quad  1 I 3 mod 2 = - i-off +!
    LOOP   #24 ?flush-tris
; ' frame-draw frame is draw-bg

: }}glue ( glue -- o )
    glue-tile new >o to tile-glue o o> ;
: }}frame ( glue color border -- o )
    frame new >o to border to frame-color to tile-glue o o> ;
: }}image ( glue color texture-xt -- o )
    image new >o is image-tex to frame-color to tile-glue
    image-tex edge mipmap cubic-mipmap o o> ;

\ text widget

5% fvalue text-shrink%
5% fvalue text-grow%

widget class
    value: text-color
    sfvalue: text-w
    value: text-font
    $value: text$
end-class text

: text! ( addr u font -- )
    to text-font to text$  +glyphs ;
: text-xy! ( -- )
    x border kerning f+ borderl f+ f+ fround penxy         sf!
    y                        raise f+ fround penxy sfloat+ sf!
    w border f2* borderl f+ f- kerning f- text-w f/ to x-scale
    text-font to font  text-color color ! ;
: text-text ( -- ) text-xy!
    text$ render-string ;
: text-!size ( -- )
    text-font to font
    text$ layout-string
    border borderv f+ bordert f+ f+ to h
    border borderv f+ f+ to d
    fdup to text-w  border f2* borderl f+ f+ to w
\    ." text sized to: " x f. y f. w f. h f. d f. cr
;
' text-text text is draw-text
' text-!size text is !size
:noname w kerning f+
    text-w text-shrink% f* text-w text-grow% f* ; text is hglue
:noname h raise f+ 0e fdup ; text is vglue
:noname d raise f- 0e fdup ; text is dglue

\ translated text

text class
    value: l-text
end-class i18n-text

: i18n-text! ( lsid font -- )
    to text-font to l-text  +lang ;

\ editable text widget

text class
    value: curpos
    value: cursize
    value: start-curpos \ selection mode
end-class edit

: edit-marking ( -- )
    cursize 0< ?EXIT  text-font to font
    w border f2* borderl f+ f- text-w fdup f0= IF  f*  ELSE   f/  THEN { f: scale }
    text$ curpos umin layout-string fdrop fdrop
    scale f* { f: w }
    setstring$ $@len IF
	setstring$ $@ layout-string fdrop fdrop scale f*
    ELSE
	text$ curpos cursize m2c:curminchars# @ umax + umin
	layout-string fdrop fdrop scale f* w f-
    THEN  fdup f0= IF  fdrop m2c:curminwidth% f@ fdup f2/ fnegate
    ELSE  0e   THEN  { f: cw f: cw- }
    x cw- f+ w f+ border f+ borderl f+ y d border borderv f+ f- f+ { f: x0 f: y0 }
    x0 cw f+ y h border borderv f+ bordert f+ f- f- { f: x1 f: y1 }
    i>off -2e to t.i0
    m2c:selectioncolor# m2c:cursorcolor# cursize 0> select @ >v
    x0 y0 >xy dup rgba>c n> 2e 2e >st v+
    x1 y0 >xy dup rgba>c n> 3e 2e >st v+
    x0 y1 >xy dup rgba>c n> 2e 3e >st v+
    x1 y1 >xy     rgba>c n> 3e 3e >st v+
    v> 2 quad  #4 ?flush-tris ;

$FFFF7FFF color, Value setstring-color

: edit-text ( -- ) edit-marking  text-xy!
    cursize 0= setstring$ $@len and IF
	text$ curpos umin render-string
	setstring-color color !
	setstring$ $@ render-string
	text-color color !
	text$ curpos safe/string render-string
    ELSE
	text$ render-string
    THEN ;
: edit-!size ( -- )
    text-font to font
    cursize 0= setstring$ $@len and IF
	text$ curpos umin layout-string { f: d f: h }
	setstring$ $@ layout-string
	h fmax to h d fmax to d  f+
	text$ curpos safe/string layout-string
	h fmax to h d fmax to d  f+  d h
    ELSE
	text$ layout-string
    THEN
    border borderv f+ bordert f+ f+ to h
    border borderv f+ f+ to d
    fdup to text-w  border f2* f+ borderl f+ to w ;
' edit-text edit is draw-text
' edit-!size edit is !size

: edit! ( addr u font -- )
    text!  text$ nip to curpos  -1 to cursize  -1 to start-curpos ;

\ password editor

edit class
    cvalue: pw-mode \ 0: hidden, 1: show last char, 2: reveal
end-class pw-edit

( '●' ) '•' Value pw-char

Variable *insflag

: text$->* ( -- oldtext$ )
    text$ over curpos + dup cursize 0 max +  0 addr text$ !@ >r
    [: { cursor cur# } bounds over >r ?DO
	    I c@ $C0 $80 within IF
		I cur# = cursize 0>= and IF
		    text$ nip curpos - to cursize
		THEN
		I xchar+ cursor = IF
		    *insflag @ IF
			I dup xchar+ over - type
		    ELSE  pw-char xemit  THEN
		    text$ nip to curpos
		ELSE  pw-char xemit  THEN
	    THEN
	LOOP
	r> cur#   = cursize 0>= and IF
	    text$ nip curpos - to cursize  THEN
    ;] addr text$ $exec
    r> ;

: pw-xt { xt -- }
    cursize >r curpos >r
    pw-mode dup 0= IF  *insflag off  THEN
    2 < IF
	text$->* >r xt catch r> addr text$ $!buf
	r> to curpos r> to cursize throw
    ELSE
	2rdrop  xt execute
    THEN ;
:noname ( -- ) ['] edit-text    pw-xt ; pw-edit is draw-text
:noname ( -- ) ['] edit-!size   pw-xt ; pw-edit is !size

\ draw wrapper

also freetype-gl
: <draw-init ( -- )
    -1e 1e >apxy  .01e 100e 100e >ap
    Ambient 1 ambient% glUniform1fv
    0e fdup fdup fdup glClearColor clear ;

: draw-init> ( -- )
    atlas texture_atlas_t-modified c@ IF
	gen-atlas-tex time( ." atlas: " .!time cr )
	0 atlas texture_atlas_t-modified c!
    THEN
    atlas-bgra texture_atlas_t-modified c@ IF
	gen-atlas-tex-bgra time( ." atlas-bgra: " .!time cr )
	0 atlas-bgra texture_atlas_t-modified c!
    THEN ;
previous

: <draw-image ( -- ) ; \ image draw, one draw call per image
: draw-image> ( -- ) ;
: <draw-text ( -- )
    program glUseProgram
    GL_TEXTURE3 glActiveTexture
    z-bias set-color+3
    atlas-scaletex
    atlas-tex
    GL_TEXTURE2 glActiveTexture
    z-bias set-color+2
    atlas-bgra-scaletex
    atlas-tex-bgra
    GL_TEXTURE0 glActiveTexture
    vi0 ; \ bg+text+marking draw, one draw call in total

\ load style into atlas-tex-bgra

atlas-region buffer: (ar)

also soil also freetype-gl

: mem>style ( addr u -- )
    atlas-tex-bgra
    over >r  0 0 0 { w^ w w^ h w^ ch# }
    w h ch# SOIL_LOAD_RGBA SOIL_load_image_from_memory
    r> free throw
    BEGIN
	atlas-bgra w @ 1+ h @ 1+ (ar) texture_atlas_get_region
	(ar) i.x (ar) i.y -1 -1 d= WHILE
	    atlas-bgra atlas-bgra# 2* dup >r to atlas-bgra#
	    r> dup texture_atlas_enlarge_texture
    REPEAT
    >r atlas-bgra (ar) i.x (ar) i.y (ar) i.w 1- (ar) i.h 1- r@ (ar) i.w 1- 2* 2*
    texture_atlas_set_region
    r> free throw  (ar) ;
: load-style ( addr u -- ivec4-addr )
    open-fpath-file throw 2drop slurp-fid mem>style ;

previous previous

: style: load-style Create here atlas-region dup allot move
  DOES> to frame# ;

"button.png" style: button1
"button2.png" style: button2
"button3.png" style: button3
' button1 >body Value slider-frame# \ set the frame number to button2 style

\ boxes

glue class
    field: childs[] \ all children
    field: box-flags
    value: aidglue \ helper glue for tables
    method resized
    method map
end-class box

: >hglue!@ ( glue -- glue' )
    aidglue ?dup-IF  .hglue!@  THEN ;
: >vglue!@ ( glue -- glue' )
    aidglue ?dup-IF  .vglue!@  THEN ;
: >dglue!@ ( glue -- glue' )
    aidglue ?dup-IF  .dglue!@  THEN ;

: do-childs { xt -- .. }
    box-flags @ box-flip# and ?EXIT
    childs[] $@ bounds U+DO
	xt I @ .execute
    cell +LOOP ;
: do-lastchild ( xt -- .. )
    childs[] $[]# ?dup-IF 1- childs[] $[] @ .execute ELSE  drop  THEN ;

:noname ( -- )
    ['] !size do-childs
    hglue hglue-c glue!
    dglue dglue-c glue!
    vglue vglue-c glue! ; box is !size

:noname ( -- ) ['] draw-init      do-childs ; box is draw-init
:noname ( -- ) ['] draw-bg        do-childs ; box is draw-bg
:noname ( -- ) ['] draw-image     do-childs ; box is draw-image
:noname ( -- ) ['] draw-text      do-childs ; box is draw-text

:noname ( -- )
    parent-w ?dup-IF  .resized \ upwards
    ELSE  !size xywhd resize     \ downwards
    THEN ;
dup widget is resized
box is resized

: +child ( o -- ) o over >o to parent-w o> childs[] >back ;
: child+ ( o -- ) o over >o to parent-w o> childs[] >stack ;
: +childs ( o1 .. on n -- ) childs[] set-stack
    o [: dup to parent-w ;] do-childs drop ;

\ glue arithmetics

box class end-class hbox \ horizontal alignment
box class
end-class vbox \ vertical alignment
box class end-class zbox \ overlay alignment

1e20 fconstant 1fil
1e40 fconstant 1fill
1e60 fconstant 1filll
1e-20 fconstant 0g \ minimum glue, needs to be bigger than zero to avoid 0/0

: fils ( f -- f' ) 1fil f* ;
: fills ( f -- f' ) 1fill f* ;
: fillls ( f -- f' ) 1filll f* ;

: 0glue ( -- t s a ) 0e 0g 0g ;
: 1glue ( -- t s a ) 0e 0g 1fil ;
: 1kglue ( -- t s a ) 0e 0g 1fill ;
: 1Mglue ( -- t s a ) 0e 0g 1filll ;

: .fil[l[l]] ( f -- )
    fdup 1fil f< IF  f.  EXIT  THEN
    1fil f/ fdup 1fil f< IF  f. ." fil" EXIT  THEN
    1fil f/ fdup 1fil f< IF  f. ." fill" EXIT  THEN
    1fil f/ f. ." filll" ;

: .glue { f: t f: s f: a -- }
    t f. s f. a .fil[l[l]] ;
: .rec { f: x f: y f: w f: h f: d -- }
    x f. y f. w f. h f. d f. ;

glue new Constant glue*l
glue new Constant glue*ll
glue new Constant glue*lll
glue new Constant glue*2
glue*l >o 1glue hglue-c glue! 0glue dglue-c glue! 1glue vglue-c glue! o>
glue*ll >o 1kglue hglue-c glue! 0glue dglue-c glue! 1glue vglue-c glue! o>
glue*lll >o 1Mglue hglue-c glue! 0glue dglue-c glue! 1glue vglue-c glue! o>
glue*2 >o 1glue f2* hglue-c glue! 0glue f2* dglue-c glue! 1glue f2* vglue-c glue! o>

: g3>2 ( t s a -- min a ) fover f+ { f: a } f- a ;
: g3>2grow ( t s a -- min a ) fnip ;
: g3>2shrink ( t s a -- min a ) fdrop ;
: ?g3>2 ( t s a flag -- min a )
    IF  g3>2grow  ELSE  g3>2shrink  THEN ;

: glue+ { f: t1 f: s1 f: a1 f: t2 f: s2 f: a2 -- t3 s3 a3 }
    \G stick two glues together
    t1 t2 f+ s1 s2 f+ a1 a2 f+ ;
: glue* { f: t1 f: s1 f: a1 f: t2 f: s2 f: a2 -- t3 s3 a3 }
    \G overlay two glues together 
    t1 t2 fmax
    t1 s1 f- t2 s2 f- fmax fover f- fnegate 0g fmax
    t1 a1 f+ t2 a2 f+ fmin 2 fpick f- 0g fmax ;
: glue-dup { f: t1 f: s1 f: a1 -- t1 s1 a1 t1 s1 a1 }
    t1 s1 a1 t1 s1 a1 ;
: glue-drop ( t s a -- ) fdrop fdrop fdrop ;
: baseglue ( -- b 0 max )
    baseline 0g 1filll ;

: hglue+ 0glue box-flags @ box-hflip# and ?EXIT [: hglue@ glue+ ;] do-childs ;
: dglue+ 0glue box-flags @ box-vflip# and ?EXIT
    [: glue-drop dglue@ ;] do-lastchild ; \ last dglue
: vglue+ 0glue box-flags @ box-vflip# and ?EXIT
    0glue [: vglue@ glue+ frot baseline fmax f-rot glue+ dglue@ ;] do-childs
    glue-drop ;

: hglue* box-flags @ box-hflip# and IF  0glue  EXIT  THEN
    1glue [: hglue@ glue* ;] do-childs ;
: dglue* box-flags @ box-hflip# and IF  0glue  EXIT  THEN
    1glue [: dglue@ glue* ;] do-childs ;
: vglue* box-flags @ box-hflip# and IF  0glue  EXIT  THEN
    1glue [: vglue@ glue* ;] do-childs ;

:noname hglue+ >hglue!@ ; hbox is hglue
:noname dglue* >dglue!@ ; hbox is dglue
:noname vglue* >vglue!@ ; hbox is vglue

:noname hglue* >hglue!@ ; vbox is hglue
:noname dglue+ >dglue!@ ; vbox is dglue
:noname vglue+ >vglue!@ ; vbox is vglue

:noname hglue* >hglue!@ ; zbox is hglue
:noname dglue* >dglue!@ ; zbox is dglue
:noname vglue* >vglue!@ ; zbox is vglue

\ add glues up for hboxes

:noname defers printdebugdata cr f.s ; is printdebugdata

: hglue-step { f: gp/a f: rg f: rd f: rx -- gp/a rg' rd' rx' }
    \g gp/a: total additonal pixels to stretch into divided by total glue
    \g rg: running glue
    \g rd: running remaining pixels
    \g rx: running x
    gp/a  rx to x
    hglue@ gp/a f0> ?g3>2 +to rg { f: xmin }
    rg fdup gp/a f*
    fdup rd f- xmin f+  fdup to w  rx f+ ;

: hbox-resize1 { f: y f: h f: d -- y h d } x y w h d resize
\    ." hchild resized: " x f. y f. w f. h f. d f. cr
    y h d ;
: hbox-resize { f: x f: y f: w f: h f: d -- }
    x y w h d widget-resize
    hglue+  w border f2* borderl f+ f- { f: wtotal }
    2 fpick wtotal f<= ?g3>2 { f: wmin f: a }
    wtotal wmin f- a f/ 0e 0e x ['] hglue-step do-childs
    fdrop fdrop fdrop fdrop
    y h d ['] hbox-resize1 do-childs  fdrop fdrop fdrop
\    ." hbox sized to: " x f. y f. w f. h f. d f. cr
;

' hbox-resize hbox is resize

\ add glues up for vboxes

: vglue-step-h { f: gp/a f: rg f: rd f: ry f: od -- gp/a rg' rd' ry' }
    \g gp/a: total additonal pixels to stretch into
    \g       by total glue to stretch into (so you can multiply with it)
    \g rg: running glue
    \g rd: running remaining pixels
    \g rx: running y
    \g od: previous descender
    gp/a
    vglue@ gp/a f0> ?g3>2 +to rg { f: ymin }
    rg fdup gp/a f* \ rd'
    fdup rd f- ymin f+   fdup to h
    baseline od f- fmax  ry f+ fdup to y ;

: vglue-step-d { f: gp/a f: rg f: rd f: ry -- gp/a rg' rd' ry' d' }
    \g gp/a: total additonal pixels to stretch into
    \g       by total glue to stretch into (so you can multiply with it)
    \g rd: running remaining pixels
    \g rg: running glue
    \g rx: running y
    \g d': this descender
    gp/a
    dglue@ gp/a f0> ?g3>2  +to rg { f: ymin }
    rg fdup gp/a f*
    fdup rd f- ymin f+ fdup to d 
    fdup ry f+ fswap ;

: vglue-step ( gp/a rd rg ry od -- gp/a rd' rg' ry' od )
    vglue-step-h vglue-step-d ;

: vbox-resize1 { f: x f: w -- x w } x y w h d resize
\    ." vchild resized: " x f. y f. w f. h f. d f. cr
    x w ;
: vbox-resize { f: x f: y f: w f: h f: d -- }
    x y w h d widget-resize
    hglue* glue-drop  vglue+ dglue+ glue+
    h d f+ border borderv f+ f2* bordert f+ f- { f: htotal }
    2 fpick htotal f<= ?g3>2 { f: hmin f: a }
    htotal hmin f- a f/ 0e 0e
    y border borderv f+ bordert f+ f+ h f- 0e ['] vglue-step do-childs
    fdrop fdrop fdrop fdrop fdrop
    x border f+ w border f2* f- borderl f- ['] vbox-resize1 do-childs fdrop fdrop
\    ." vbox sized to: " x f. y f. w f. h f. d f. cr
;

' vbox-resize vbox is resize

: zbox-resize1 { f: x f: y f: w f: h f: d -- x y w h d }
    x y w h d resize
\    ." zchild resized: " x f. y f. w f. h f. d f. cr
    x y w h d ;

: zbox-resize { f: x f: y f: w f: h f: d -- }
    x y w h d widget-resize
    x border f+ borderl f+ y border borderv f+ bordert f+ f+ w border f2* f-
    h border borderv f+ bordert f+ f- d border borderv f+ f-
    ['] zbox-resize1 do-childs
    fdrop fdrop fdrop fdrop fdrop
\    ." zbox sized to: " x f. y f. w f. h f. d f. cr
;

' zbox-resize zbox is resize

$10 stack: box-depth \ this $10 here is no real limit
: {{ ( -- ) depth box-depth >stack ;
: }} ( n1 .. nm -- n1 .. nm m ) depth box-depth stack> - ;
: }}h ( n1 .. nm -- hbox ) }} hbox new >o +childs o o> ;
: }}v ( n1 .. nm -- vbox ) }} vbox new >o +childs o o> ;
: }}z ( n1 .. nm -- zbox ) }} zbox new >o +childs o o> ;

\ tab helper glues

helper-glue class
    glues +field htab-c
    glues +field htab-co
end-class htab-glue

:noname ( -- )
    htab-c htab-co glues move
    1glue htab-c glue! ; htab-glue is aidglue0
:noname ( -- flag )
    htab-c glues htab-co over str= ; htab-glue is aidglue=
:noname ( glue -- glue' )
    htab-c glue@ glue* glue-dup htab-c glue!
    fdrop fdrop 0g fdup ; \ don't allow shrinking/growing
htab-glue is hglue!@

\ draw everything

: widget-draw ( o:widget -- )  time( ." draw:  " .!time cr )
    <draw-init      draw-init      draw-init>   time( ." init:  " .!time cr )
    <draw-text draw-bg draw-text   render>      time( ." text:  " .!time cr )
    <draw-image     draw-image     draw-image>  time( ." img:   " .!time cr )
    sync time( ." sync:  " .!time cr ) ;

\ viewport: Draw into a frame buffer

vbox class
    sfvalue: vp-x \ x offset of visible part of viewport
    sfvalue: vp-y \ y offset of visible part of viewport
    sfvalue: vp-w \ width inside viewport
    sfvalue: vp-h \ height inside viewport
    defer: vp-tex
    value: vp-fb
    value: vp-glue \ glue object
    field: vp-need
end-class viewport

: vp-top ( o:vp -- )    vp-h h f- to vp-y ;
: vp-bottom ( o:vp -- )        0e to vp-y ;
: vp-left ( o:vp -- )          0e to vp-x ;
: vp-right ( o:vp -- )  vp-w w f- to vp-x ;

: vp-needed ( xt -- )
    need-mask >r vp-need to need-mask
    catch r> to need-mask throw ;

: draw-vpchilds ( -- )
    <draw-text      ['] draw-bg        do-childs
                    ['] draw-text      do-childs render>
    <draw-image     ['] draw-image     do-childs draw-image>
;

1 sfloats buffer: vp-ambient%  1.0e vp-ambient% sf!
1 sfloats buffer: vp-saturate% 1.0e vp-saturate% sf!

: <draw-vp ( -- )
    vp-h vp-w f>s f>s vp-fb >framebuffer
    Ambient 1 vp-ambient% glUniform1fv
    Saturate 1 vp-saturate% glUniform1fv
    0e fdup fdup fdup glClearColor clear
    .01e 100e 100e vp-h vp-w f>s f>s >apwh ;
: draw-vp> ( -- )
    Ambient 1 ambient% glUniform1fv
    Saturate 1 saturate% glUniform1fv
    -1e 1e >apxy  .01e 100e 100e >ap ;

:noname
    [: ?sync ?config or ;] vp-needed IF
	<draw-vp  ['] draw-init do-childs  draw-init>
	draw-vpchilds
	0>framebuffer draw-vp>
	[: -sync -config ;] vp-needed
    THEN ; viewport is draw-init
:noname ( -- )
    z-bias set-color+ vp-tex
    xywh >xyxy { f: x1 f: y1 f: x2 f: y2 -- }
    vp-x vp-w f/ vp-y vp-h f/ w vp-w f/ h vp-h f/ { f: s0 f: t0 f: s1 f: t1 }
    vi0 i>off  $FFFFFFFF >v
    x1 y1 >xy dup rgba>c n> s0       t0 t1 f+ >st v+
    x2 y1 >xy dup rgba>c n> s0 s1 f+ t0 t1 f+ >st v+
    x1 y2 >xy dup rgba>c n> s0       t0       >st v+
    x2 y2 >xy     rgba>c n> s0 s1 f+ t0       >st v+
    v> 2 quad
    render-bgra> ; viewport is draw-image
: vp-!size ( -- )
    ['] !size do-childs
    hglue* hglue-c glue!
    dglue+ dglue-c glue!
    vglue+ vglue-c glue!
    w hglue-c df@ fmax
    fdup vp-w f<> to vp-w
    h d f+ dglue-c df@ vglue-c df@ f+ fmax
    fdup vp-h f<> to vp-h
    vp-h h d f+ f- vp-y fmin fdup vp-y f<> to vp-y
    vp-w w f- vp-x fmin fdup vp-x f<> to vp-x
    or or or IF ['] +sync vp-needed THEN ;
' vp-!size viewport is !size
:noname ( -- )
    ['] +sync vp-needed [ box :: resized ] ; viewport is resized

4 buffer: texwh

:noname { f: x f: y f: w f: h f: d -- }
    x y w h d widget-resize
    vp-!size
    vp-tex vp-fb IF
	vp-w f>s vp-h f>s 2dup 0 -rot GL_RGBA texture-map \ just resize
	GL_RENDERBUFFER GL_DEPTH_COMPONENT16 2swap glRenderbufferStorage
    ELSE
	vp-w f>s vp-h f>s GL_RGBA new-textbuffer to vp-fb
    THEN
    0e vp-h vp-w vp-h 0e vbox-resize
    x y w h d widget-resize
; viewport is resize
' noop viewport is draw-bg
' noop viewport is draw-text
:noname vp-glue .hglue >hglue!@ ; viewport is hglue
:noname vp-glue .dglue >dglue!@ ; viewport is dglue
:noname vp-glue .vglue >vglue!@ ; viewport is vglue
:noname vp-glue .hglue@ ; viewport is hglue@
:noname vp-glue .dglue@ ; viewport is dglue@
:noname vp-glue .vglue@ ; viewport is vglue@

: }}vp ( b:n1 .. b:nm glue vp-tex -- viewport ) { g t }
    }} viewport new >o +childs t is vp-tex g to vp-glue o o> ;

\ slider (simple composit object)

tile class \ tile-glue here is the viewport link
end-class vslider-part \ slider part

:noname w 0g fdup ; vslider-part is hglue
:noname d 0g fdup ; vslider-part is dglue
:noname d 0g tile-glue >o h d f+ o> ; vslider-part is vglue
' frame-draw vslider-part is draw-bg

vslider-part class
end-class vslider-partu \ upper part

vslider-part class
end-class vslider-partd \ lower part

' 0glue vslider-partu is hglue
' 0glue vslider-partu is dglue
:noname 0e fdup tile-glue >o vp-h vp-y f- h d f+ f- o> ; vslider-partu is vglue
' noop vslider-partu is draw-bg

' 0glue vslider-partd is hglue
' 0glue vslider-partd is dglue
:noname 0e fdup tile-glue .vp-y ; vslider-partd is vglue
' noop vslider-partd is draw-bg

\ vslider

Create vslider-parts
vslider-partu , vslider-part , vslider-partd ,

tile class \ tile-glue here is the viewport link
end-class hslider-part \ slider part

:noname d f2* 0g tile-glue .w ; hslider-part is hglue
:noname h 0g fdup ; hslider-part is vglue
:noname d 0g fdup ; hslider-part is dglue
' frame-draw hslider-part is draw-bg

hslider-part class
end-class hslider-partl \ left part

:noname 0g fdup tile-glue .vp-x ; hslider-partl is hglue
' noop hslider-partl is draw-bg

hslider-part class
end-class hslider-partr

:noname 0g fdup tile-glue >o vp-w vp-x f- o> ; hslider-partr is hglue
' noop hslider-partr is draw-bg

Create hslider-parts
hslider-partl , hslider-part , hslider-partr ,

\ slider top

$7F7F7FFF color, Value slider-color
$7F7F7FFF color, Value slider-fgcolor
8e FValue slider-border

: slider { parts viewport-link f: sw f: sd f: sh -- ou os od }
    parts 3 cells bounds DO
	I @ new >o slider-frame# to frame#
	slider-fgcolor to frame-color  slider-border to border  0e to baseline
	viewport-link to tile-glue  sw to w  sd to d  sh to h  o o>
    cell +LOOP ;

\ top widget and actors

0 Value top-widget
: top-act ( -- o ) top-widget .act ;

require actors.fs
require animation.fs

\ composite objects

: hslider ( viewport-link sd sh -- o )
    >r {{ glue*l slider-color slider-border }}frame dup .button3
    {{ hslider-parts r@ 0g frot frot slider
    over r> swap hslider[] }}h box[]
    }}z box[] ;
: vslider ( viewport-link sw sd -- o )
    >r {{ glue*l slider-color slider-border }}frame dup .button3
    {{ vslider-parts r@ 0g slider
    over r> swap vslider[] }}v box[]
    }}z box[] ;

: htop-resize ( -- )
    !size 0e 1e dh* 1e dw* 1e dh* 0e resize time( ." resize: " .!time cr ) ;

: widgets-loop ( -- )
    [IFDEF] hidekb  hidekb [THEN]  enter-minos
    1 level# +!  top-widget .widget-draw
    BEGIN  0 looper-to# anims[] $@len ?sync or select
	#looper  time( ." looper: " .!time cr )
	[IFDEF] android  ?config-changer  [THEN]
	anims[] $@len IF  animations true  ELSE  ?sync  THEN
	IF  top-widget >o htop-resize widget-draw time( ." animate: " .!time cr )
	    o>
	    -sync  THEN
	?keyboard IF
	    [IFDEF] showkb showkb [THEN]
	    -keyboard  THEN
    level# @ 0= UNTIL  leave-minos  +sync ;

previous previous previous
set-current
