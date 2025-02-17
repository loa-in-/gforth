\ Forth output paging add-on (like more(1))

\ Authors: Anton Ertl, Bernd Paysan
\ Copyright (C) 1996,2000,2003,2007,2016 Free Software Foundation, Inc.

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


\ This add-on is for those poor souls whose terminals cannot scroll
\ back but who want to read the output of 'words' at their leisure.

\ currently this is very primitive: it just counts newlines, and only
\ allows continuing for another page (and of course, terminating
\ processing by sending a signal (^C))

\ Some things to do:
\ allow continuing for one line (Enter)
\ count lines produced by wraparound (note tabs and backspaces)
\ allow continuing silently
\ fancy features like searching, scrollback etc.

\ one more-or-less simple way to achieve all this is to
\ popen("less","w") and output there. Before getting the next `key`,
\ we would perform a pclose. This idea due to Marcel Hendrix.

require termsize.fs

variable last-#lines 0 last-#lines !

: (more-attr!) ( attr -- )
    dup input-color = IF
	1 last-#lines !
    THEN
    defers attr! ;

: (more-emit) ( c -- )
    dup defers emit
    #lf =
    if
	1 last-#lines +!
	last-#lines @ rows >=
	if
	    ." ... more ?" key drop 1 last-#lines !
	    10 backspaces 10 spaces 10 backspaces
	endif
    endif ;

: (more-type) ( c-addr u -- )
    bounds
    ?DO
	I c@ emit
    LOOP ;

' (more-type) ' (more-emit) action-of cr action-of form output: more-out

action-of page
action-of at-xy
action-of at-deltaxy

more-out

' (more-attr!) is attr!
is at-deltaxy
is at-xy
is page
