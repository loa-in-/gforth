\ KERNAL.FS    ANS figFORTH kernal                     17dec92py
\ $ID:
\ Idea and implementation: Bernd Paysan (py)
\ Copyright 1992 by the ANSI figForth Development Group

\ Log:  ', '- usw. durch [char] ... ersetzt
\       man sollte die unterschiedlichen zahlensysteme
\       mit $ und & zumindest im interpreter weglassen
\       schon erledigt!
\       11may93jaw
\ name>         0= nicht vorhanden              17may93jaw
\               nfa can be lfa or nfa!
\ find          splited into find and (find)
\               (find) for later use            17may93jaw
\ search        replaced by lookup because
\               it is a word of the string wordset
\                                               20may93jaw
\ postpone      added immediate                 21may93jaw
\ to            added immediate                 07jun93jaw
\ cfa, header   put "here lastcfa !" in
\               cfa, this is more logical
\               and noname: works wothout
\               extra "here lastcfa !"          08jun93jaw
\ (parse-white) thrown out
\ refill        added outer trick
\               to show there is something
\               going on                        09jun93jaw
\ leave ?leave  somebody forgot UNLOOP!!!       09jun93jaw
\ leave ?leave  unloop thrown out
\               unloop after loop is used       10jun93jaw

HEX

\ Bit string manipulation                              06oct92py

Create bits  80 c, 40 c, 20 c, 10 c, 8 c, 4 c, 2 c, 1 c,
DOES> ( n -- )  + c@ ;

: >bit  ( addr n -- c-addr mask )  8 /mod rot + swap bits ;
: +bit  ( addr n -- )  >bit over c@ or swap c! ;

: relinfo ( -- addr )  forthstart dup @ + ;
: >rel  ( addr -- n )  forthstart - ;
: relon ( addr -- )  relinfo swap >rel cell / +bit ;

\ here allot , c, A,                                   17dec92py

: dp	( -- addr )  dpp @ ;
: here  ( -- here )  dp @ ;
: allot ( n -- )     dp +! ;
: c,    ( c -- )     here 1 chars allot c! ;
: ,     ( x -- )     here cell allot  ! ;
: 2,    ( w1 w2 -- ) \ general
    here 2 cells allot 2! ;

: aligned ( addr -- addr' )
  [ cell 1- ] Literal + [ -1 cells ] Literal and ;
: align ( -- )          here dup aligned swap ?DO  bl c,  LOOP ;

: faligned ( addr -- f-addr )
  [ 1 floats 1- ] Literal + [ -1 floats ] Literal and ;

: falign ( -- )
  here dup faligned swap
  ?DO
      bl c,
  LOOP ;



: A!    ( addr1 addr2 -- )  dup relon ! ;
: A,    ( addr -- )     here cell allot A! ;

\ on off                                               23feb93py

: on  ( addr -- )  true  swap ! ;
: off ( addr -- )  false swap ! ;

\ name> found                                          17dec92py

: (name>)  ( nfa -- cfa )    count  $1F and  +  aligned ;
: name>    ( nfa -- cfa )    cell+
  dup  (name>) swap  c@ $80 and 0= IF  @ THEN ;

: found ( nfa -- cfa n )  cell+
  dup c@ >r  (name>) r@ $80 and  0= IF  @       THEN
                  -1 r@ $40 and     IF  1-      THEN
                     r> $20 and     IF  negate  THEN  ;

\ (find)                                               17dec92py

\ : (find) ( addr count nfa1 -- nfa2 / false )
\   BEGIN  dup  WHILE  dup >r
\          cell+ count $1F and dup >r 2over r> =
\          IF  -text  0= IF  2drop r> EXIT  THEN
\          ELSE  2drop drop  THEN  r> @
\   REPEAT nip nip ;

\ place bounds                                         13feb93py

: place  ( addr len to -- ) over >r  rot over 1+  r> move c! ;
: bounds ( beg count -- end beg )  over + swap ;

\ input stream primitives                              23feb93py

: tib   >tib @ ;
Defer source
: (source) ( -- addr count ) tib #tib @ ;
' (source) IS source

\ (word)                                               22feb93py

: scan   ( addr1 n1 char -- addr2 n2 )  >r
  BEGIN  dup  WHILE  over c@ r@ <>  WHILE  1 /string
  REPEAT  THEN  rdrop ;
: skip   ( addr1 n1 char -- addr2 n2 )  >r
  BEGIN  dup  WHILE  over c@ r@  =  WHILE  1 /string
  REPEAT  THEN  rdrop ;

: (word) ( addr1 n1 char -- addr2 n2 )
  dup >r skip 2dup r> scan  nip - ;

\ (word) should fold white spaces
\ this is what (parse-white) does

\ word parse                                           23feb93py

: parse-word  ( char -- addr len )
  source 2dup >r >r >in @ /string
  rot dup bl = IF  drop (parse-white)  ELSE  (word)  THEN
  2dup + r> - 1+ r> min >in ! ;
: word   ( char -- addr )
  parse-word here place  bl here count + c!  here ;

: parse    ( char -- addr len )
  >r  source  >in @ /string  over  swap r>  scan >r
  over - dup r> IF 1+ THEN  >in +! ;

\ name                                                 13feb93py

: capitalize ( addr -- addr )
  dup count chars bounds
  ?DO  I c@ toupper I c! 1 chars +LOOP ;
: (name)  ( -- addr )  bl word ;
: (cname) ( -- addr )  bl word capitalize ;

\ Literal                                              17dec92py

: Literal  ( n -- )  state @ IF postpone lit  , THEN ;
                                                      immediate
: ALiteral ( n -- )  state @ IF postpone lit A, THEN ;
                                                      immediate

: char   ( 'char' -- n )  bl word char+ c@ ;
: [char] ( 'char' -- n )  char postpone Literal ; immediate
' [char] Alias Ascii immediate

: (compile) ( -- )  r> dup cell+ >r @ A, ;
: postpone ( "name" -- )
  name find dup 0= abort" Can't compile "
  0> IF  A,  ELSE  postpone (compile) A,  THEN ;
                                             immediate restrict

\ Use (compile) for the old behavior of compile!

\ digit?                                               17dec92py

: digit?   ( char -- digit true/ false )
  base @ $100 =
  IF
    true EXIT
  THEN
  toupper [char] 0 - dup 9 u> IF
    [ 'A '9 1 + -  ] literal -
    dup 9 u<= IF
      drop false EXIT
    THEN
  THEN
  dup base @ u>= IF
    drop false EXIT
  THEN
  true ;

: accumulate ( +d0 addr digit - +d1 addr )
  swap >r swap  base @  um* drop rot  base @  um* d+ r> ;
: >number ( d addr count -- d addr count )
  0 ?DO  count digit? WHILE  accumulate  LOOP 0
  ELSE  1- I' I - UNLOOP  THEN ;

\ number? number                                       23feb93py

Create bases   10 ,   2 ,   A , 100 ,
\              16     2    10   Zeichen
\ !! this saving and restoring base is an abomination! - anton
: getbase ( addr u -- addr' u' )  over c@ [char] $ - dup 4 u<
  IF  cells bases + @ base ! 1 /string  ELSE  drop  THEN ;
: number?  ( string -- string 0 / n -1 )  base @ >r
  dup count over c@ [char] - = dup >r  IF 1 /string  THEN
  getbase  dpl on  0 0 2swap
  BEGIN  dup >r >number dup  WHILE  dup r> -  WHILE
         dup dpl ! over c@ [char] . =  WHILE
         1 /string
  REPEAT  THEN  2drop 2drop rdrop false r> base ! EXIT  THEN
  2drop rot drop rdrop r> IF dnegate THEN
  dpl @ dup 0< IF  nip  THEN  r> base ! ;
: s>d ( n -- d ) dup 0< ;
: number ( string -- d )
  number? ?dup 0= abort" ?"  0< IF s>d THEN ;

\ space spaces ud/mod                                  21mar93py
decimal
Create spaces  bl 80 times \ times from target compiler! 11may93jaw
DOES>   ( u -- )  swap
        0 max 0 ?DO  I' I - &80 min 2dup type  +LOOP  drop ;
hex
: space   1 spaces ;

: ud/mod ( ud1 u2 -- urem udquot )  >r 0 r@ um/mod r> swap >r
                                    um/mod r> ;

: pad    ( -- addr )
  here [ $20 8 2* cells + 2 + cell+ ] Literal + aligned ;

\ hold <# #> sign # #s                                 25jan92py

: hold    ( char -- )         pad cell - -1 chars over +! @ c! ;

: <#                          pad cell - dup ! ;

: #>      ( 64b -- addr +n )  2drop pad cell - dup @ tuck - ;

: sign    ( n -- )            0< IF  [char] - hold  THEN ;

: #       ( +d1 -- +d2 )    base @ 2 max ud/mod rot 9 over <
  IF [ char A char 9 - 1- ] Literal +  THEN  [char] 0 + hold ;

: #s      ( +d -- 0 0 )         BEGIN  # 2dup d0=  UNTIL ;

\ print numbers                                        07jun92py

: d.r      >r tuck  dabs  <# #s  rot sign #>
           r> over - spaces  type ;

: ud.r     >r <# #s #> r> over - spaces type ;

: .r       >r s>d r> d.r ;
: u.r      0 swap ud.r ;

: d.       0 d.r space ;
: ud.      0 ud.r space ;

: .        s>d d. ;
: u.       0 ud. ;

\ catch throw                                          23feb93py
\ bounce                                                08jun93jaw

\ !! allow the user to add rollback actions    anton
\ !! use a separate exception stack?           anton

: lp@ ( -- addr )
 laddr# [ 0 , ] ;

: catch ( x1 .. xn xt -- y1 .. ym 0 / z1 .. zn error )
  >r sp@ r> swap >r       \ don't count xt! jaw
  fp@ >r
  lp@ >r
  handler @ >r
  rp@ handler !
  execute
  r> handler ! rdrop rdrop rdrop 0 ;

: throw ( y1 .. ym error/0 -- y1 .. ym / z1 .. zn error )
    ?DUP IF
	[ here 4 cells ! ]
	handler @ rp!
	r> handler !
	r> lp!
	r> fp!
	r> swap >r sp! r>
    THEN ;

\ Bouncing is very fine,
\ programming without wasting time...   jaw
: bounce ( y1 .. ym error/0 -- y1 .. ym error / y1 .. ym )
\ a throw without data or fp stack restauration
  ?DUP IF
    handler @ rp!
    r> handler !
    r> lp!
    rdrop
    rdrop
  THEN ;

\ ?stack                                               23feb93py

: ?stack ( ?? -- ?? )  sp@ s0 @ > IF  -4 throw  THEN ;
\ ?stack should be code -- it touches an empty stack!

\ interpret                                            10mar92py

Defer parser
Defer name      ' (name) IS name
Defer notfound

: no.extensions  ( string -- )  IF  &-13 bounce  THEN ;

' no.extensions IS notfound

: interpret
  BEGIN  ?stack name dup c@  WHILE  parser  REPEAT drop ;

\ interpreter compiler                                 30apr92py

: interpreter  ( name -- ) find ?dup
  IF  1 and  IF execute  EXIT THEN  -&14 throw  THEN
  number? 0= IF  notfound THEN ;

' interpreter  IS  parser

: compiler     ( name -- ) find  ?dup
  IF  0> IF  execute EXIT THEN compile, EXIT THEN number? dup
  IF  0> IF  swap postpone Literal  THEN  postpone Literal
  ELSE  drop notfound  THEN ;

: [     ['] interpreter  IS parser state off ; immediate
: ]     ['] compiler     IS parser state on  ;

\ locals stuff needed for control structures

: compile-lp+! ( n -- )
    dup negate locals-size +!
    0 over = if
    else -4 over = if postpone -4lp+!
    else  8 over = if postpone  8lp+!
    else 16 over = if postpone 16lp+!
    else postpone lp+!# dup ,
    then then then then drop ;

: adjust-locals-size ( n -- )
    \ sets locals-size to n and generates an appropriate lp+!
    locals-size @ swap - compile-lp+! ;


here 0 , \ just a dummy, the real value of locals-list is patched into it in glocals.fs
AConstant locals-list \ acts like a variable that contains
		      \ a linear list of locals names


variable dead-code \ true if normal code at "here" would be dead

: unreachable ( -- )
\ declares the current point of execution as unreachable
 dead-code on ;

\ locals list operations

: common-list ( list1 list2 -- list3 )
\ list1 and list2 are lists, where the heads are at higher addresses than
\ the tail. list3 is the largest sublist of both lists.
 begin
   2dup u<>
 while
   2dup u>
   if
     swap
   then
   @
 repeat
 drop ;

: sub-list? ( list1 list2 -- f )
\ true iff list1 is a sublist of list2
 begin
   2dup u<
 while
   @
 repeat
 = ;

: list-size ( list -- u )
\ size of the locals frame represented by list
 0 ( list n )
 begin
   over 0<>
 while
   over
   cell+ name> >body @ max
   swap @ swap ( get next )
 repeat
 faligned nip ;

: set-locals-size-list ( list -- )
 dup locals-list !
 list-size locals-size ! ;

: check-begin ( list -- )
\ warn if list is not a sublist of locals-list
 locals-list @ sub-list? 0= if
   \ !! print current position
   ." compiler was overly optimistic about locals at a BEGIN" cr
   \ !! print assumption and reality
 then ;

\ Control Flow Stack
\ orig, etc. have the following structure:
\ type ( defstart, live-orig, dead-orig, dest, do-dest, scopestart) ( TOS )
\ address (of the branch or the instruction to be branched to) (second)
\ locals-list (valid at address) (third)

\ types
0 constant defstart
1 constant live-orig
2 constant dead-orig
3 constant dest \ the loopback branch is always assumed live
4 constant do-dest
5 constant scopestart

: def? ( n -- )
    defstart <> abort" unstructured " ;

: orig? ( n -- )
 dup live-orig <> swap dead-orig <> and abort" expected orig " ;

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

: CS-PICK ( ... u -- ... destu )
 1+ cs-item-size * 1- >r
 r@ pick  r@ pick  r@ pick
 rdrop
 dup non-orig? ;

: CS-ROLL ( destu/origu .. dest0/orig0 u -- .. dest0/orig0 destu/origu )
 1+ cs-item-size * 1- >r
 r@ roll r@ roll r@ roll
 rdrop
 dup cs-item? ; 

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

: ?struc      ( flag -- )       abort" unstructured " ;
: sys?        ( sys -- )        dup 0= ?struc ;
: >mark ( -- orig )
 cs-push-orig 0 , ;
: >resolve    ( addr -- )        here over - swap ! ;
: <resolve    ( addr -- )        here - , ;

: BUT       1 cs-roll ;                      immediate restrict
: YET       0 cs-pick ;                       immediate restrict

\ Structural Conditionals                              12dec92py

: AHEAD ( -- orig )
 POSTPONE branch >mark unreachable ; immediate restrict

: IF ( -- orig )
 POSTPONE ?branch >mark ; immediate restrict

: ?DUP-IF \ general
\ This is the preferred alternative to the idiom "?DUP IF", since it can be
\ better handled by tools like stack checkers
    POSTPONE ?dup POSTPONE if ;       immediate restrict
: ?DUP-NOT-IF \ general
    POSTPONE ?dup POSTPONE 0= POSTPONE if ; immediate restrict

: THEN ( orig -- )
    dup orig?
    dead-code @
    if
	dead-orig =
	if
	    >resolve drop
	else
	    >resolve set-locals-size-list dead-code off
	then
    else
	dead-orig =
	if
	    >resolve drop
	else \ both live
	    over list-size adjust-locals-size
	    >resolve
	    locals-list @ common-list dup list-size adjust-locals-size
	    locals-list !
	then
    then ; immediate restrict

' THEN alias ENDIF immediate restrict \ general
\ Same as "THEN". This is what you use if your program will be seen by
\ people who have not been brought up with Forth (or who have been
\ brought up with fig-Forth).

: ELSE ( orig1 -- orig2 )
    POSTPONE ahead
    1 cs-roll
    POSTPONE then ; immediate restrict


: BEGIN ( -- dest )
    dead-code @ if
	\ set up an assumption of the locals visible here
	\ currently we just take the top cs-item
	\ it would be more intelligent to take the top orig
	\   but that can be arranged by the user
	dup defstart <> if
	    dup cs-item?
	    2 pick
	else
	    0
	then
	set-locals-size-list
    then
    cs-push-part dest
    dead-code off ; immediate restrict

\ AGAIN (the current control flow joins another, earlier one):
\ If the dest-locals-list is not a subset of the current locals-list,
\ issue a warning (see below). The following code is generated:
\ lp+!# (current-local-size - dest-locals-size)
\ branch <begin>
: AGAIN ( dest -- )
    dest?
    over list-size adjust-locals-size
    POSTPONE branch
    <resolve
    check-begin
    unreachable ; immediate restrict

\ UNTIL (the current control flow may join an earlier one or continue):
\ Similar to AGAIN. The new locals-list and locals-size are the current
\ ones. The following code is generated:
\ ?branch-lp+!# <begin> (current-local-size - dest-locals-size)
: until-like ( list addr xt1 xt2 -- )
    \ list and addr are a fragment of a cs-item
    \ xt1 is the conditional branch without lp adjustment, xt2 is with
    >r >r
    locals-size @ 2 pick list-size - dup if ( list dest-addr adjustment )
	r> drop r> compile,
	swap <resolve ( list adjustment ) ,
    else ( list dest-addr adjustment )
	drop
	r> compile, <resolve
	r> drop
    then ( list )
    check-begin ;

: UNTIL ( dest -- )
    dest? ['] ?branch ['] ?branch-lp+!# until-like ; immediate restrict

: WHILE ( dest -- orig dest )
    POSTPONE if
    1 cs-roll ; immediate restrict

: REPEAT ( orig dest -- )
    POSTPONE again
    POSTPONE then ; immediate restrict


\ counted loops

\ leave poses a little problem here
\ we have to store more than just the address of the branch, so the
\ traditional linked list approach is no longer viable.
\ This is solved by storing the information about the leavings in a
\ special stack.

\ !! remove the fixed size limit. 'Tis not hard.
20 constant leave-stack-size
create leave-stack  60 cells allot
Avariable leave-sp  leave-stack 3 cells + leave-sp !

: clear-leave-stack ( -- )
    leave-stack leave-sp ! ;

\ : leave-empty? ( -- f )
\  leave-sp @ leave-stack = ;

: >leave ( orig -- )
    \ push on leave-stack
    leave-sp @
    dup [ leave-stack 60 cells + ] Aliteral
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

: DONE ( orig -- )  drop >r drop
    \ !! the original done had ( addr -- )
    begin
	leave>
	over r@ u>=
    while
	POSTPONE then
    repeat
    >leave rdrop ; immediate restrict

: LEAVE ( -- )
    POSTPONE ahead
    >leave ; immediate restrict

: ?LEAVE ( -- )
    POSTPONE 0= POSTPONE if
    >leave ; immediate restrict

: DO ( -- do-sys )
    POSTPONE (do)
    POSTPONE begin drop do-dest
    ( 0 0 0 >leave ) ; immediate restrict

: ?DO ( -- do-sys )
    ( 0 0 0 >leave )
    POSTPONE (?do)
    >mark >leave
    POSTPONE begin drop do-dest ; immediate restrict

: FOR ( -- do-sys )
    POSTPONE (for)
    POSTPONE begin drop do-dest
    ( 0 0 0 >leave ) ; immediate restrict

\ LOOP etc. are just like UNTIL

: loop-like ( do-sys xt1 xt2 -- )
    >r >r 0 cs-pick swap cell - swap 1 cs-roll r> r> rot do-dest?
    until-like  POSTPONE done  POSTPONE unloop ;

: LOOP ( do-sys -- )
 ['] (loop) ['] (loop)-lp+!# loop-like ; immediate restrict

: +LOOP ( do-sys -- )
 ['] (+loop) ['] (+loop)-lp+!# loop-like ; immediate restrict

\ A symmetric version of "+LOOP". I.e., "-high -low ?DO -inc S+LOOP"
\ will iterate as often as "high low ?DO inc S+LOOP". For positive
\ increments it behaves like "+LOOP". Use S+LOOP instead of +LOOP for
\ negative increments.
: S+LOOP ( do-sys -- )
 ['] (s+loop) ['] (s+loop)-lp+!# loop-like ; immediate restrict

: NEXT ( do-sys -- )
 ['] (next) ['] (next)-lp+!# loop-like ; immediate restrict

\ Structural Conditionals                              12dec92py

: EXIT ( -- )
    0 adjust-locals-size
    POSTPONE ;s
    unreachable ; immediate restrict

: ?EXIT ( -- )
     POSTPONE if POSTPONE exit POSTPONE then ; immediate restrict

\ Strings                                              22feb93py

: ," ( "string"<"> -- ) [char] " parse
  here over char+ allot  place align ;
: "lit ( -- addr )
  r> r> dup count + aligned >r swap >r ;               restrict
: (.")     "lit count type ;                           restrict
: (S")     "lit count ;                                restrict
: SLiteral postpone (S") here over char+ allot  place align ;
                                             immediate restrict
: S"       [char] " parse  state @ IF  postpone SLiteral  THEN ;
                                             immediate
: ."       state @  IF    postpone (.") ,"  align
                    ELSE  [char] " parse type  THEN  ;  immediate
: (        [char] ) parse 2drop ;                       immediate
: \        source >in ! drop ;                          immediate

\ error handling                                       22feb93py
\ 'abort thrown out!                                   11may93jaw

: (abort")      "lit >r IF  r> "error ! -2 throw  THEN
                rdrop ;
: abort"        postpone (abort") ," ;        immediate restrict

\ Header states                                        23feb93py

: flag! ( 8b -- )
    last @ dup 0= abort" last word was headerless"
    cell+ tuck c@ xor swap c! ;
: immediate     $20 flag! ;
: restrict      $40 flag! ;
\ ' noop alias restrict

\ Header                                               23feb93py

\ input-stream, nextname and noname are quite ugly (passing
\ information through global variables), but they are useful for dealing
\ with existing/independent defining words

defer header

: name,  ( "name" -- )
    name c@
    dup $1F u> &-19 and throw ( is name too long? )
    1+ chars allot align ;
: input-stream-header ( "name" -- )
    \ !! this is f83-implementation-dependent
    align here last !  -1 A,
    name, $80 flag! ;

: input-stream ( -- )  \ general
\ switches back to getting the name from the input stream ;
    ['] input-stream-header IS header ;

' input-stream-header IS header

\ !! make that a 2variable
create nextname-buffer 32 chars allot

: nextname-header ( -- )
    \ !! f83-implementation-dependent
    nextname-buffer count
    align here last ! -1 A,
    dup c,  here swap chars  dup allot  move  align
    $80 flag!
    input-stream ;

\ the next name is given in the string
: nextname ( c-addr u -- ) \ general
    dup $1F u> &-19 and throw ( is name too long? )
    nextname-buffer c! ( c-addr )
    nextname-buffer count move
    ['] nextname-header IS header ;

: noname-header ( -- )
    0 last !
    input-stream ;

: noname ( -- ) \ general
\ the next defined word remains anonymous. The xt of that word is given by lastxt
    ['] noname-header IS header ;

: lastxt ( -- xt ) \ general
\ xt is the execution token of the last word defined. The main purpose of this word is to get the xt of words defined using noname
    lastcfa @ ;

: Alias    ( cfa "name" -- )
  Header reveal , $80 flag! ;

: name>string ( nfa -- addr count )
 cell+ count $1F and ;

Create ???  0 , 3 c, char ? c, char ? c, char ? c,
: >name ( cfa -- nfa )
 $21 cell do
   dup i - count $9F and + aligned over $80 + = if
     i - cell - unloop exit
   then
 cell +loop
 drop ??? ( wouldn't 0 be better? ) ;

\ indirect threading                                   17mar93py

: cfa,     ( code-address -- )
    here lastcfa !
    here  0 A, 0 ,  code-address! ;
: compile, ( xt -- )		A, ;
: !does    ( addr -- )		lastcfa @ does-code! ;
: (;code)  ( R: addr -- )	r> /does-handler + !does ;
: dodoes,  ( -- )
  here /does-handler allot does-handler! ;

\ direct threading is implementation dependent

: Create    Header reveal [ :dovar ] Literal cfa, ;

\ DOES>                                                17mar93py

: DOES>  ( compilation: -- )
    state @
    IF
	;-hook postpone (;code) dodoes,
    ELSE
	dodoes, here !does 0 ]
    THEN 
    :-hook ; immediate

\ Create Variable User Constant                        17mar93py

: Variable  Create 0 , ;
: AVariable Create 0 A, ;
: 2VARIABLE ( "name" -- ) \ double
    create 0 , 0 , ;
    
: User      Variable ;
: AUser     AVariable ;

: (Constant)  Header reveal [ :docon ] Literal cfa, ;
: Constant  (Constant) , ;
: AConstant (Constant) A, ;

: 2CONSTANT
    create ( w1 w2 "name" -- )
        2,
    does> ( -- w1 w2 )
        2@ ;
    
\ IS Defer What's Defers TO                            24feb93py

: Defer
  Create ( -- ) 
    ['] noop A,
  DOES> ( ??? )
    @ execute ;

: IS ( addr "name" -- )
    ' >body
    state @
    IF    postpone ALiteral postpone !  
    ELSE  !
    THEN ;  immediate
' IS Alias TO immediate

: What's ( "name" -- addr )  ' >body
  state @ IF  postpone ALiteral postpone @  ELSE  @  THEN ;
                                             immediate
: Defers ( "name" -- )  ' >body @ compile, ;
                                             immediate restrict

\ : ;                                                  24feb93py

defer :-hook ( sys1 -- sys2 )
defer ;-hook ( sys2 -- sys1 )

: : ( -- colon-sys )  Header [ :docol ] Literal cfa, defstart ] :-hook ;
: ; ( colon-sys -- )  ;-hook ?struc postpone exit reveal postpone [ ;
  immediate restrict

: :noname ( -- xt colon-sys )
    0 last !
    here [ :docol ] Literal cfa, 0 ] :-hook ;

\ Search list handling                                 23feb93py

AVariable current

: last?   ( -- false / nfa nfa )    last @ ?dup ;
: (reveal) ( -- )
  last?
  IF
      dup @ 0<
      IF
	current @ @ over ! current @ !
      ELSE
	drop
      THEN
  THEN ;

\ object oriented search list                          17mar93py

\ word list structure:
\ struct
\   1 cells: field find-method   \ xt: ( c_addr u wid -- name-id )
\   1 cells: field reveal-method \ xt: ( -- )
\   1 cells: field rehash-method \ xt: ( wid -- )
\   \ !! what else
\ end-struct wordlist-map-struct

\ struct
\   1 cells: field wordlist-id \ not the same as wid; representation depends on implementation
\   1 cells: field wordlist-map \ pointer to a wordlist-map-struct
\   1 cells: field wordlist-link \ link field to other wordlists
\   1 cells: field wordlist-extend \ points to wordlist extensions (eg hash)
\ end-struct wordlist-struct

: f83find      ( addr len wordlist -- nfa / false )  @ (f83find) ;
: f83casefind  ( addr len wordlist -- nfa / false )  @ (f83casefind) ;

\ Search list table: find reveal
Create f83search       ' f83casefind A,  ' (reveal) A,  ' drop A,

: caps-name       ['] (cname) IS name  ['] f83find     f83search ! ;
: case-name       ['] (name)  IS name  ['] f83casefind f83search ! ;
: case-sensitive  ['] (name)  IS name  ['] f83find     f83search ! ;

Create forth-wordlist  NIL A, G f83search T A, NIL A, NIL A,
AVariable search       G forth-wordlist search T !
G forth-wordlist current T !

: (search-wordlist)  ( addr count wid -- nfa / false )
  dup ( @ swap ) cell+ @ @ execute ;

: search-wordlist  ( addr count wid -- 0 / xt +-1 )
  (search-wordlist) dup  IF  found  THEN ;

Variable warnings  G -1 warnings T !

: check-shadow  ( addr count wid -- )
\ prints a warning if the string is already present in the wordlist
\ !! should be refined so the user can suppress the warnings
 >r 2dup 2dup r> (search-wordlist) warnings @ and ?dup if
   ." redefined " name>string 2dup type
   compare 0<> if
     ."  with " type
   else
     2drop
   then
   space space EXIT
 then
 2drop 2drop ;

: find   ( addr -- cfa +-1 / string false )  dup
  count search @ search-wordlist  dup IF  rot drop  THEN ;

: reveal ( -- )
 last? if
   name>string current @ check-shadow
 then
 current @ cell+ @ cell+ @ execute ;

: rehash  ( wid -- )  dup cell+ @ cell+ cell+ @ execute ;

: '    ( "name" -- addr )  name find 0= no.extensions ;
: [']  ( "name" -- addr )  ' postpone ALiteral ; immediate
\ Input                                                13feb93py

07 constant #bell
08 constant #bs
7F constant #del
0D constant #cr                \ the newline key code
0A constant #lf

: bell  #bell emit ;

: backspaces  0 ?DO  #bs emit  LOOP ;
: >string  ( span addr pos1 -- span addr pos1 addr2 len )
  over 3 pick 2 pick chars /string ;
: type-rest ( span addr pos1 -- span addr pos1 back )
  >string tuck type ;
: (del)  ( max span addr pos1 -- max span addr pos2 )
  1- >string over 1+ -rot move
  rot 1- -rot  #bs emit  type-rest bl emit 1+ backspaces ;
: (ins)  ( max span addr pos1 char -- max span addr pos2 )
  >r >string over 1+ swap move 2dup chars + r> swap c!
  rot 1+ -rot type-rest 1- backspaces 1+ ;
: ?del ( max span addr pos1 -- max span addr pos2 0 )
  dup  IF  (del)  THEN  0 ;
: (ret)  type-rest drop true space ;
: back  dup  IF  1- #bs emit  ELSE  #bell emit  THEN 0 ;
: forw 2 pick over <> IF  2dup + c@ emit 1+  ELSE  #bell emit  THEN 0 ;

Create crtlkeys
  ] false false back  false  false false forw  false
    ?del  false (ret) false  false (ret) false false
    false false false false  false false false false
    false false false false  false false false false [

: decode ( max span addr pos1 key -- max span addr pos2 flag )
  dup #del = IF  drop #bs  THEN  \ del is rubout
  dup bl <   IF  cells crtlkeys + @ execute  EXIT  THEN
  >r 2over = IF  rdrop bell 0 EXIT  THEN
  r> (ins) 0 ;

\ decode should better use a table for control key actions
\ to define keyboard bindings later

: accept   ( addr len -- len )
  dup 0< IF    abs over dup 1 chars - c@ tuck type 
\ this allows to edit given strings
         ELSE  0  THEN rot over
  BEGIN  key decode  UNTIL
  2drop nip ;

\ Output                                               13feb93py

DEFER type      \ defer type for a output buffer or fast
                \ screen write

\ : (type) ( addr len -- )
\   bounds ?DO  I c@ emit  LOOP ;

' (TYPE) IS Type

DEFER Emit

' (Emit) IS Emit

\ : form  ( -- rows cols )  &24 &80 ;
\ form should be implemented using TERMCAPS or CURSES
\ : rows  form drop ;
\ : cols  form nip  ;

\ Query                                                07apr93py

: refill ( -- flag )
  tib /line
  loadfile @ ?dup
  IF    dup file-position throw linestart 2!
        read-line throw
  ELSE  linestart @ IF 2drop false EXIT THEN
        accept true
  THEN
  1 loadline +!
  swap #tib ! 0 >in ! ;

: Query  ( -- )  0 loadfile ! refill drop ;

\ File specifiers                                       11jun93jaw


\ 1 c, here char r c, 0 c,                0 c, 0 c, char b c, 0 c,
\ 2 c, here char r c, char + c, 0 c,
\ 2 c, here char w c, char + c, 0 c, align
4 Constant w/o
2 Constant r/w
0 Constant r/o

\ BIN WRITE-LINE                                        11jun93jaw

\ : bin           dup 1 chars - c@
\                 r/o 4 chars + over - dup >r swap move r> ;

: bin  1+ ;

create nl$ 1 c, A c, 0 c, \ gnu includes usually a cr in dos
                           \ or not unix environments if
                           \ bin is not selected

: write-line    dup >r write-file ?dup IF r> drop EXIT THEN
                nl$ count r> write-file ;

\ include-file                                         07apr93py

: include-file ( i*x fid -- j*x )
  linestart @ >r loadline @ >r loadfile @ >r
  blk @ >r >tib @ >r  #tib @ dup >r  >in @ >r

  >tib +! loadfile !
  0 loadline ! blk off
  BEGIN  refill  WHILE  interpret  REPEAT
  loadfile @ close-file throw

  r> >in !  r> #tib !  r> >tib ! r> blk !
  r> loadfile ! r> loadline ! r> linestart ! ;

: included ( i*x addr u -- j*x )
    loadfilename 2@ >r >r
    dup allocate throw over loadfilename 2!
    over loadfilename 2@ move
    r/o open-file throw include-file
    \ don't free filenames; they don't take much space
    \ and are used for debugging
    r> r> loadfilename 2! ;

\ HEX DECIMAL                                           2may93jaw

: decimal a base ! ;
: hex     10 base ! ;

\ DEPTH                                                 9may93jaw

: depth ( -- +n )  sp@ s0 @ swap - cell / ;

\ INCLUDE                                               9may93jaw

: include  ( "file" -- )
  bl word count included ;

\ RECURSE                                               17may93jaw

: recurse ( -- )
    lastxt compile, ; immediate restrict
: recursive ( -- )
    reveal ; immediate

\ */MOD */                                              17may93jaw

: */mod >r m* r> sm/rem ;

: */ */mod nip ;

\ EVALUATE                                              17may93jaw

: evaluate ( c-addr len -- )
  linestart @ >r loadline @ >r loadfile @ >r
  blk @ >r >tib @ >r  #tib @ dup >r  >in @ >r

  >tib +! dup #tib ! >tib @ swap move
  >in off blk off loadfile off -1 linestart !

  BEGIN  interpret  >in @ #tib @ u>= UNTIL

  r> >in !  r> #tib !  r> >tib ! r> blk !
  r> loadfile ! r> loadline ! r> linestart ! ;


: abort -1 throw ;

\+ environment? true ENV" CORE"
\ core wordset is now complete!

\ Quit                                                 13feb93py

Defer 'quit
Defer .status
: prompt        state @ IF ."  compiled" EXIT THEN ."  ok" ;
: (quit)        BEGIN .status cr query interpret prompt AGAIN ;
' (quit) IS 'quit

\ DOERROR (DOERROR)                                     13jun93jaw

: dec. ( n -- )
    \ print value in decimal representation
    base @ decimal swap . base ! ;

: typewhite ( addr u -- )
    \ like type, but white space is printed instead of the characters
    0 ?do
	dup i + c@ 9 = if \ check for tab
	    9
	else
	    bl
	then
	emit
    loop
    drop ;

DEFER DOERROR

: (DoError) ( throw-code -- )
    LoadFile @
    IF
	cr loadfilename 2@ type ." :" Loadline @ dec.
    THEN
    cr source type cr
    source drop >in @ -trailing ( throw-code line-start index2 )
    here c@ 1F min dup >r - 0 max ( throw-code line-start index1 )
    typewhite
    r> 1 max 0 ?do \ we want at least one "^", even if the length is 0
	." ^"
    loop
    dup -2 =
    IF 
	"error @ ?dup
	IF
	    cr count type 
	THEN
	drop
    ELSE
	.error
    THEN
    normal-dp dpp ! ;

' (DoError) IS DoError

: quit   r0 @ rp! handler off >tib @ >r
  BEGIN
    postpone [
    ['] 'quit CATCH dup
  WHILE
    DoError r@ >tib !
  REPEAT
  drop r> >tib ! ;

\ Cold                                                 13feb93py

\ : .name ( name -- ) cell+ count $1F and type space ;
\ : words  listwords @
\          BEGIN  @ dup  WHILE  dup .name  REPEAT drop ;

: >len  ( cstring -- addr n )  100 0 scan 0 swap 100 - /string ;
: arg ( n -- addr count )  cells argv @ + @ >len ;
: #!       postpone \ ;  immediate

Variable env
Variable argv
Variable argc

: get-args ( -- )  #tib off
  argc @ 1 ?DO  I arg 2dup source + swap move
                #tib +! drop  bl source + c! 1 #tib +!  LOOP
  >in off #tib @ 0<> #tib +! ;

: script? ( -- flag )  0 arg 1 arg dup 3 pick - /string compare 0= ;

: cold ( -- )  
    argc @ 1 >
    IF  script?
	IF
	    1 arg ['] included
	ELSE
	    get-args ['] interpret
	THEN
	catch ?dup
	IF
	    dup >r DoError cr r> (bye)
	THEN
    THEN
    cr ." GNU Forth 0.0alpha, Copyright (C) 1994 Free Software Foundation"
    cr ." GNU Forth comes with ABSOLUTELY NO WARRANTY; for details type `license'" 
    cr quit ;

: boot ( **env **argv argc -- )
  argc ! argv ! env !  main-task up!
  sp@ dup s0 ! $10 + >tib ! rp@ r0 !  fp@ f0 !  cold ;

: bye  cr 0 (bye) ;

\ **argv may be scanned by the C starter to get some important
\ information, as -display and -geometry for an X client FORTH
\ or space and stackspace overrides

\ 0 arg contains, however, the name of the program.
