\ Structural Conditionals                              12dec92py

\ Authors: Anton Ertl, Bernd Paysan, Neal Crook, Jens Wilke
\ Copyright (C) 1995,1996,1997,2000,2003,2004,2007,2010,2011,2012,2014,2015,2016,2017,2018 Free Software Foundation, Inc.

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

here 0 , \ just a dummy, the real value of locals-list is patched into it in glocals.fs
AValue locals-list \ acts like a variable that contains
		      \ a linear list of locals names
0 value locals-wordlist

variable dead-code \ true if normal code at "here" would be dead
variable backedge-locals
    \ contains the locals list that BEGIN will assume to be live on
    \ the back edge if the BEGIN is unreachable from above. Set by
    \ ASSUME-LIVE, reset by UNREACHABLE.

: UNREACHABLE ( -- ) \ gforth
    \ declares the current point of execution as unreachable
    dead-code on
    0 backedge-locals ! ; immediate

: ASSUME-LIVE ( orig -- orig ) \ gforth
    \ used immediatly before a BEGIN that is not reachable from
    \ above.  causes the BEGIN to assume that the same locals are live
    \ as at the orig point
    dup orig?
    2 pick backedge-locals ! ; immediate
    
\ Control Flow Stack
\ orig, etc. have the following structure:
\ type ( defstart, live-orig, dead-orig, dest, do-dest, scopestart) ( TOS )
\ address (of the branch or the instruction to be branched to) (second)
\ locals-list (valid at address) (third)

\ types
[IFUNDEF] defstart 
0 constant defstart	\ usally defined in comp.fs
[THEN]
1 constant live-orig
2 constant dead-orig
3 constant dest \ the loopback branch is always assumed live
4 constant do-dest
5 constant scopestart

: orig? ( n -- )
    dead-orig 1+ live-orig within abort" expected orig " ;

: dest? ( n -- )
    dest <> abort" expected dest " ;

: do-dest? ( n -- )
    do-dest <> abort" expected do-dest " ;

: scope? ( n -- )
    scopestart <> abort" expected scope " ;

: non-orig? ( n -- )
    dest scopestart 1+ within 0= abort" expected dest, do-dest or scope" ;

: cs-item? ( n -- )
    live-orig scopestart 1+ within 0= abort" expected control flow stack item" ;

3 constant cs-item-size

: CS-PICK ( orig0/dest0 orig1/dest1 ... origu/destu u -- ... orig0/dest0 ) \ tools-ext c-s-pick
    1+ cs-item-size * 1- >r
    r@ pick  r@ pick  r@ pick
    rdrop
    dup cs-item? ;

: CS-ROLL ( destu/origu .. dest0/orig0 u -- .. dest0/orig0 destu/origu ) \ tools-ext c-s-roll
    1+ cs-item-size * 1- >r
    r@ roll r@ roll r@ roll
    rdrop
    dup cs-item? ; 

: CS-DROP ( dest -- ) \ gforth
    cs-item? 2drop ;

: cs-push-part ( -- list addr )
    locals-list @ here ;

: cs-push-orig ( -- orig )
    cs-push-part dead-code @
    if
	dead-orig
    else
	live-orig
    then ;   

\ Structural Conditionals                              12dec92py

defer other-control-flow ( -- )
\ hook for control-flow stuff that's not handled by begin-like etc.
defer if-like
\ hook for if-like control flow not handled by other-control-flow

: ?struc      ( tag -- )
    defstart <> &-22 and throw ;
: ?colon-sys  ( ... xt tag -- )
    ?struc execute ;

: >mark ( -- orig )
    cs-push-orig 0 , other-control-flow ;
: >mark? ( -- orig )
    >mark if-like ;
: >resolve    ( addr -- )
    basic-block-end
    here swap ! ;
: <resolve    ( addr -- )        , ;

: BUT
    1 cs-roll ;                      immediate restrict
: YET
    0 cs-pick ;                      immediate restrict
: NOPE
    cs-drop ;                        immediate restrict

\ Structural Conditionals                              12dec92py

: AHEAD ( compilation -- orig ; run-time -- ) \ tools-ext
    \G At run-time, execution continues after the @code{THEN} that
    \G consumes the @i{orig}.
    POSTPONE branch  >mark  POSTPONE unreachable ; immediate restrict

: IF ( compilation -- orig ; run-time f -- ) \ core
    \G At run-time, if @i{f}=0, execution continues after the
    \G @code{THEN} (or @code{ELSE}) that consumes the @i{orig},
    \G otherwise right after the @code{IF} (@pxref{Selection}).
    POSTPONE ?branch >mark? ; immediate restrict

: ?DUP-IF ( compilation -- orig ; run-time n -- n| ) \ gforth	question-dupe-if
    \G This is the preferred alternative to the idiom "@code{?DUP
    \G IF}", since it can be better handled by tools like stack
    \G checkers. Besides, it's faster.
    POSTPONE ?dup-?branch >mark? ;       immediate restrict

: ?DUP-0=-IF ( compilation -- orig ; run-time n -- n| ) \ gforth	question-dupe-zero-equals-if
    POSTPONE ?dup-0=-?branch >mark? ;       immediate restrict

Defer then-like ( orig -- )
: cs>addr ( orig/dest -- )  drop >resolve drop ;
' cs>addr IS then-like

: THEN ( compilation orig -- ; run-time -- ) \ core
    \G The @code{IF}, @code{AHEAD}, @code{ELSE} or @code{WHILE} that
    \G pushed @i{orig} jumps right after the @code{THEN}
    \G (@pxref{Selection}).
    dup orig?  then-like ; immediate restrict

' THEN alias ENDIF ( compilation orig -- ; run-time -- ) \ gforth
\G Same as @code{THEN}.
immediate restrict

: ELSE ( compilation orig1 -- orig2 ; run-time -- ) \ core
    \G At run-time, execution continues after the @code{THEN} that
    \G consumes the @i{orig}; the @code{IF}, @code{AHEAD}, @code{ELSE}
    \G or @code{WHILE} that pushed @i{orig1} jumps right after the
    \G @code{ELSE}.  (@pxref{Selection}).
    POSTPONE ahead  dead-code off
    1 cs-roll
    POSTPONE then ; immediate restrict

Defer begin-like ( -- )
' lits, IS begin-like

: BEGIN ( compilation -- dest ; run-time -- ) \ core
    \G The @code{UNTIL}, @code{AGAIN} or @code{REPEAT} that consumes
    \G the @i{dest} jumps right behind the @code{BEGIN} (@pxref{Simple
    \G Loops}).
    begin-like cs-push-part dest
    basic-block-end ; immediate restrict

Defer again-like ( dest -- addr )
' nip IS again-like

: AGAIN ( compilation dest -- ; run-time -- ) \ core-ext
    \G At run-time, execution continues after the @code{BEGIN} that
    \G produced the @i{dest} (@pxref{Simple Loops}).
    dest? again-like  POSTPONE branch  <resolve ; immediate restrict

Defer until-like ( list addr xt1 xt2 -- )
:noname ( list addr xt1 xt2 -- )
    drop compile, <resolve drop ;
IS until-like

: UNTIL ( compilation dest -- ; run-time f -- ) \ core
    \G At run-time, if @i{f}=0, execution continues after the
    \G @code{BEGIN} that produced @i{dest}, otherwise right after
    \G the @code{UNTIL} (@pxref{Simple Loops}).
    dest? ['] ?branch ['] ?branch-lp+!# until-like ; immediate restrict

: WHILE ( compilation dest -- orig dest ; run-time f -- ) \ core
    \G At run-time, if @i{f}=0, execution continues after the
    \G @code{REPEAT} (or @code{THEN} or @code{ELSE}) that consumes the
    \G @i{orig}, otherwise right after the @code{WHILE} (@pxref{Simple
    \G Loops}).
    POSTPONE if
    1 cs-roll ; immediate restrict

: REPEAT ( compilation orig dest -- ; run-time -- ) \ core
    \G At run-time, execution continues after the @code{BEGIN} that
    \G produced the @i{dest}; the @code{WHILE}, @code{IF},
    \G @code{AHEAD} or @code{ELSE} that pushed @i{orig} jumps right
    \G after the @code{REPEAT}.  (@pxref{Simple Loops}).
    POSTPONE again
    POSTPONE then ; immediate restrict

\ not clear if this should really go into Gforth's kernel...

: CONTINUE ( dest-sys j*sys -- dest-sys j*sys ) \ gforth
    \g jump to the next outer BEGIN
    depth 0 ?DO  I pick dest = IF
	    I cs-item-size / cs-pick postpone AGAIN
	    UNLOOP  EXIT  THEN
    cs-item-size +LOOP
    true abort" no BEGIN found" ; immediate restrict

\ counted loops

\ leave poses a little problem here
\ we have to store more than just the address of the branch, so the
\ traditional linked list approach is no longer viable.
\ This is solved by storing the information about the leavings in a
\ special stack.

\ !! remove the fixed size limit. 'Tis not hard.
40 constant leave-stack-size
create leave-stack  leave-stack-size cs-item-size * cells allot
Avariable leave-sp  leave-stack cs-item-size cells + leave-sp !

: clear-leave-stack ( -- )
    leave-stack leave-sp ! ;

\ : leave-empty? ( -- f )
\  leave-sp @ leave-stack = ;

: >leave ( orig -- )
    \ push on leave-stack
    leave-sp @
    dup [ leave-stack leave-stack-size cs-item-size * cells + ] Aliteral
    >= abort" leave-stack full"
    tuck ! cell+
    tuck ! cell+
    tuck ! cell+
    leave-sp ! ;

: leave> ( -- orig )
    \ pop from leave-stack
    leave-sp @
    dup leave-stack <= IF
       drop 0 0 0  EXIT  THEN
    cell - dup @ swap
    cell - dup @ swap
    cell - dup @ swap
    leave-sp ! ;

: DONE ( compilation orig -- ; run-time -- ) \ gforth
    \g resolves all LEAVEs up to the compilaton orig (from a BEGIN)
    drop >r drop
    begin
	leave>
	over r@ u>=
    while
	POSTPONE then
    repeat
    >leave rdrop ; immediate restrict

: LEAVE ( compilation -- ; run-time loop-sys -- ) \ core
    \G @xref{Counted Loops}.
    POSTPONE ahead >leave ; immediate compile-only

: ?LEAVE ( compilation -- ; run-time f | f loop-sys -- ) \ gforth	question-leave
    \G @xref{Counted Loops}.
    POSTPONE 0= POSTPONE if
    >leave ; immediate restrict

: DO ( compilation -- do-sys ; run-time w1 w2 -- loop-sys ) \ core
    \G @xref{Counted Loops}.
    POSTPONE (do)
    POSTPONE begin drop do-dest
    ( 0 0 0 >leave ) ; immediate restrict

: ?do-like ( -- do-sys )
    ( 0 0 0 >leave )
    >mark >leave
    POSTPONE begin drop do-dest ;

: ?DO ( compilation -- do-sys ; run-time w1 w2 -- | loop-sys )	\ core-ext	question-do
    \G @xref{Counted Loops}.
    POSTPONE (?do) ?do-like ; immediate restrict

: +DO ( compilation -- do-sys ; run-time n1 n2 -- | loop-sys )	\ gforth	plus-do
    \G @xref{Counted Loops}.
    POSTPONE (+do) ?do-like ; immediate restrict

: U+DO ( compilation -- do-sys ; run-time u1 u2 -- | loop-sys )	\ gforth	u-plus-do
    \G @xref{Counted Loops}.
    POSTPONE (u+do) ?do-like ; immediate restrict

: -DO ( compilation -- do-sys ; run-time n1 n2 -- | loop-sys )	\ gforth	minus-do
    \G @xref{Counted Loops}.
    POSTPONE (-do) ?do-like ; immediate restrict

: U-DO ( compilation -- do-sys ; run-time u1 u2 -- | loop-sys )	\ gforth	u-minus-do
    \G @xref{Counted Loops}.
    POSTPONE (u-do) ?do-like ; immediate restrict

: FOR ( compilation -- do-sys ; run-time u -- loop-sys )	\ gforth
    \G @xref{Counted Loops}.
    POSTPONE (for)
    POSTPONE begin drop do-dest
    ( 0 0 0 >leave ) ; immediate restrict

\ LOOP etc. are just like UNTIL

: loop-like ( do-sys xt1 xt2 -- )
    >r >r 0 cs-pick swap cell - swap 1 cs-roll r> r> rot do-dest?
    until-like  POSTPONE done  POSTPONE unloop ;

: LOOP ( compilation do-sys -- ; run-time loop-sys1 -- | loop-sys2 )	\ core
    \G @xref{Counted Loops}.
 ['] (loop) ['] (loop)-lp+!# loop-like ; immediate restrict

: +LOOP ( compilation do-sys -- ; run-time loop-sys1 n -- | loop-sys2 )	\ core	plus-loop
    \G @xref{Counted Loops}.
 ['] (+loop) ['] (+loop)-lp+!# loop-like ; immediate restrict

\ !! should the compiler warn about +DO..-LOOP?
: -LOOP ( compilation do-sys -- ; run-time loop-sys1 u -- | loop-sys2 )	\ gforth	minus-loop
    \G @xref{Counted Loops}.
 ['] (-loop) ['] (-loop)-lp+!# loop-like ; immediate restrict

\ A symmetric version of "+LOOP". I.e., "-high -low ?DO -inc S+LOOP"
\ will iterate as often as "high low ?DO inc S+LOOP". For positive
\ increments it behaves like "+LOOP". Use S+LOOP instead of +LOOP for
\ negative increments.
: S+LOOP ( compilation do-sys -- ; run-time loop-sys1 n -- | loop-sys2 )	\ gforth-obsolete	s-plus-loop
    \G @xref{Counted Loops}.
 ['] (s+loop) ['] (s+loop)-lp+!# loop-like ; immediate restrict

: NEXT ( compilation do-sys -- ; run-time loop-sys1 -- | loop-sys2 ) \ gforth
    \G @xref{Counted Loops}.
 ['] (next) ['] (next)-lp+!# loop-like ; immediate restrict

\ Structural Conditionals                              12dec92py

Defer exit-like ( -- )
' noop IS exit-like

: EXIT ( compilation -- ; run-time nest-sys -- ) \ core
\G Return to the calling definition; usually used as a way of
\G forcing an early return from a definition. Before
\G @code{EXIT}ing you must clean up the return stack and
\G @code{UNLOOP} any outstanding @code{?DO}...@code{LOOP}s.
\G Use @code{;s} for a tickable word that behaves like @code{exit}
\G in the absence of locals.
    exit-like
    POSTPONE ;s
    basic-block-end
    POSTPONE unreachable ; immediate compile-only

: ?EXIT ( -- ) ( compilation -- ; run-time nest-sys f -- | nest-sys ) \ gforth
    POSTPONE if POSTPONE exit POSTPONE then ; immediate restrict

: execute-exit ( compilation -- ; run-time xt nest-sys -- ) \ gforth
    \G Execute @code{xt} and return from the current definition, in a
    \G tail-call-optimized way: The return address @code{nest-sys} and
    \G the locals are deallocated before executing @code{xt}.
    exit-like
    POSTPONE execute-;s
    basic-block-end
    POSTPONE unreachable ; immediate compile-only

\ scope endscope

: scope ( compilation  -- scope ; run-time  -- ) \ gforth
    cs-push-part scopestart ; immediate

defer adjust-locals-list ( wid -- )
' drop is adjust-locals-list

: endscope ( compilation scope -- ; run-time  -- ) \ gforth
    scope?
    drop  adjust-locals-list ; immediate

\ quotations
: wrap@ ( -- wrap-sys )
    vtsave latest latestnt leave-sp @ locals-wordlist ( unlocal-state @ ) ;
: wrap! ( wrap-sys -- )
    ( unlocal-state ! ) to locals-wordlist leave-sp ! lastnt ! last ! vtrestore ;

: (int-;]) ( some-sys lastxt -- ) >r vt, wrap! r> ;
: (;]) ( some-sys lastxt -- )
    >r
    ] postpone ENDSCOPE vt,
    locals-list !
    postpone THEN
    wrap!
    r> postpone Literal ;

: int-[: ( -- flag colon-sys )
    wrap@ ['] (int-;]) :noname ;
: comp-[: ( -- quotation-sys flag colon-sys )
    wrap@
    postpone AHEAD
    locals-list @ locals-list off
    postpone SCOPE
    ['] (;])  :noname  ;
' int-[: ' comp-[: interpret/compile: [: ( compile-time: -- quotation-sys flag colon-sys ) \ gforth bracket-colon
\G Starts a quotation

: ;] ( compile-time: quotation-sys -- ; run-time: -- xt ) \ gforth semi-bracket
    \g ends a quotation
    POSTPONE ; swap execute ( xt ) ; immediate

