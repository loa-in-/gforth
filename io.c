/* direct key io driver; signal handler

  Copyright (C) 1995 Free Software Foundation, Inc.

  This file is part of Gforth.

  Gforth is free software; you can redistribute it and/or
  modify it under the terms of the GNU General Public License
  as published by the Free Software Foundation; either version 2
  of the License, or (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

  The following is stolen from the readline library for bash
*/

/*
   Use -D_POSIX_VERSION for POSIX systems.
*/

#include "config.h"
#include <unistd.h>

#ifdef apollo
#define _POSIX_VERSION
#endif

#include <stdio.h>
#include <signal.h>
#include <string.h>
#include <sys/types.h>
#if !defined(apollo) && !defined(MSDOS)
#include <sys/ioctl.h>
#endif
#include <fcntl.h>
#include <sys/file.h>
#include <setjmp.h>
#include "forth.h"
#include "io.h"

#ifndef MSDOS
#if defined (__GNUC__)
#  define alloca __builtin_alloca
#else
#  if defined (sparc) || defined (HAVE_ALLOCA_H)
#    include <alloca.h>
#  endif
#endif

#define NEW_TTY_DRIVER
#define HAVE_BSD_SIGNALS
/*
#ifndef apollo
#define USE_XON_XOFF
#endif
*/

#define HANDLE_SIGNALS

/* Some USG machines have BSD signal handling (sigblock, sigsetmask, etc.) */
#if defined (USG) && !defined (hpux)
#undef HAVE_BSD_SIGNALS
#endif

/* System V machines use termio. */
#if !defined (_POSIX_VERSION)
#  if defined (USG) || defined (hpux) || defined (Xenix) || defined (sgi) || defined (DGUX) || defined (ultrix) || defined (Solaris)
#    undef NEW_TTY_DRIVER
#    define TERMIO_TTY_DRIVER
#    include <termio.h>
#    if !defined (TCOON)
#      define TCOON 1
#    endif
#  endif /* USG || hpux || Xenix || sgi || DUGX || ultrix*/
#endif /* !_POSIX_VERSION */

/* Posix systems use termios and the Posix signal functions. */
#if defined (_POSIX_VERSION)
#  if !defined (TERMIOS_MISSING)
#    undef NEW_TTY_DRIVER
#    define TERMIOS_TTY_DRIVER
#    include <termios.h>
#  endif /* !TERMIOS_MISSING */
#  define HAVE_POSIX_SIGNALS
#  if !defined (O_NDELAY)
#    define O_NDELAY O_NONBLOCK	/* Posix-style non-blocking i/o */
#  endif /* O_NDELAY */
#endif /* _POSIX_VERSION */

/* Other (BSD) machines use sgtty. */
#if defined (NEW_TTY_DRIVER)
#include <sgtty.h>
#endif

/* Define _POSIX_VDISABLE if we are not using the `new' tty driver and
   it is not already defined.  It is used both to determine if a
   special character is disabled and to disable certain special
   characters.  Posix systems should set to 0, USG systems to -1. */
#if !defined (NEW_TTY_DRIVER) && !defined (_POSIX_VDISABLE)
#  if defined (_POSIX_VERSION)
#    define _POSIX_VDISABLE 0
#  else /* !_POSIX_VERSION */
#    define _POSIX_VDISABLE -1
#  endif /* !_POSIX_VERSION */
#endif /* !NEW_TTY_DRIVER && !_POSIX_VDISABLE */

#include <errno.h>
/* extern int errno; */

#if defined (SHELL)
#  include <posixstat.h>
#else
#  include <sys/stat.h>
#endif /* !SHELL */
/* #define HACK_TERMCAP_MOTION */

#if defined (USG) && defined (hpux)
#  if !defined (USGr3)
#    define USGr3
#  endif /* USGr3 */
#endif /* USG && hpux */

#if (defined (_POSIX_VERSION) || defined (USGr3)) && !defined(apollo)
#  include <dirent.h>
#  define direct dirent
#  if defined (_POSIX_VERSION)
#    define D_NAMLEN(d) (strlen ((d)->d_name))
#  else /* !_POSIX_VERSION */
#    define D_NAMLEN(d) ((d)->d_reclen)
#  endif /* !_POSIX_VERSION */
#else /* !_POSIX_VERSION && !USGr3 */
#  define D_NAMLEN(d) ((d)->d_namlen)
#  if !defined (USG)
#    include <sys/dir.h>
#  else /* USG */
#    if defined (Xenix)
#      include <sys/ndir.h>
#    else /* !Xenix */
#      include <ndir.h>
#    endif /* !Xenix */
#  endif /* USG */
#endif /* !POSIX_VERSION && !USGr3 */

#if defined (USG) && defined (TIOCGWINSZ)
#  include <sys/stream.h>
#  if defined (USGr4) || defined (USGr3)
#    if defined (Symmetry) || defined (_SEQUENT_)
#      include <sys/pte.h>
#    else
#      include <sys/ptem.h>
#    endif /* !Symmetry || _SEQUENT_ */
#  endif /* USGr4 */
#endif /* USG && TIOCGWINSZ */

#if defined (TERMIOS_TTY_DRIVER)
static struct termios otio;
#else
static struct termio otio;
#endif /* !TERMIOS_TTY_DRIVER */

/* Non-zero means echo characters as they are read. */
int readline_echoing_p = 1;

/* The character that can generate an EOF.  Really read from
   the terminal driver... just defaulted here. */

#ifndef CTRL
#define CTRL(key)	((key)-'@')
#endif

static int eof_char = CTRL ('D');

/* **************************************************************** */
/*								    */
/*		      Saving and Restoring the TTY	    	    */
/*								    */
/* **************************************************************** */

/* Non-zero means that the terminal is in a prepped state. */
int terminal_prepped = 0;

#if defined (NEW_TTY_DRIVER)

/* Standard flags, including ECHO. */
static int original_tty_flags = 0;

/* Local mode flags, like LPASS8. */
static int local_mode_flags = 0;

/* Terminal characters.  This has C-s and C-q in it. */
static struct tchars original_tchars;

/* Local special characters.  This has the interrupt characters in it. */
#if defined (TIOCGLTC)
static struct ltchars original_ltchars;
#endif

/* Bind KEY to FUNCTION.  Returns non-zero if KEY is out of range. */

#if defined (TIOCGETC)
#if defined (USE_XON_XOFF)

int
bind_key (key, function)
     int key;
     Function *function;
{
  if (key < 0)
    return (key);

  if (key > 127 && key < 256)
    {
      if (keymap[ESC].type == ISKMAP)
	{
	  Keymap escmap = (Keymap)keymap[ESC].function;

	  key -= 128;
	  escmap[key].type = ISFUNC;
	  escmap[key].function = function;
	  return (0);
	}
      return (key);
    }

  keymap[key].type = ISFUNC;
  keymap[key].function = function;
 return (0);
}
#endif
#endif

/* We use this to get and set the tty_flags. */
static struct sgttyb the_ttybuff;

#if defined (USE_XON_XOFF)
/* If the terminal was in xoff state when we got to it, then xon_char
   contains the character that is supposed to start it again. */
static int xon_char, xoff_state;
#endif /* USE_XON_XOFF */

/* **************************************************************** */
/*								    */
/*			Bogus Flow Control      		    */
/*								    */
/* **************************************************************** */

restart_output (count, key)
     int count, key;
{
  int fildes = fileno (stdin);
#if defined (TIOCSTART)
#if defined (apollo)
  ioctl (&fildes, TIOCSTART, 0);
#else
  ioctl (fildes, TIOCSTART, 0);
#endif /* apollo */

#else
#  if defined (TERMIOS_TTY_DRIVER)
        tcflow (fildes, TCOON);
#  else
#    if defined (TCXONC)
        ioctl (fildes, TCXONC, TCOON);
#    endif /* TCXONC */
#  endif /* !TERMIOS_TTY_DRIVER */
#endif /* TIOCSTART */
}

/* Put the terminal in CBREAK mode so that we can detect key presses. */
void prep_terminal ()
{
  int tty = fileno (stdin);
#if defined (HAVE_BSD_SIGNALS)
  int oldmask;
#endif /* HAVE_BSD_SIGNALS */

  if (terminal_prepped)
    return;

  if (!isatty(tty)) {      /* added by MdG */
    terminal_prepped = 1;      /* added by MdG */
    return;      /* added by MdG */
  }      /* added by MdG */
   
  oldmask = sigblock (sigmask (SIGINT));

  /* We always get the latest tty values.  Maybe stty changed them. */
  ioctl (tty, TIOCGETP, &the_ttybuff);
  original_tty_flags = the_ttybuff.sg_flags;

  readline_echoing_p = (original_tty_flags & ECHO);

#if defined (TIOCLGET)
  ioctl (tty, TIOCLGET, &local_mode_flags);
#endif

#if !defined (ANYP)
#  define ANYP (EVENP | ODDP)
#endif

  /* If this terminal doesn't care how the 8th bit is used,
     then we can use it for the meta-key.  We check by seeing
     if BOTH odd and even parity are allowed. */
  if (the_ttybuff.sg_flags & ANYP)
    {
#if defined (PASS8)
      the_ttybuff.sg_flags |= PASS8;
#endif

      /* Hack on local mode flags if we can. */
#if defined (TIOCLGET) && defined (LPASS8)
      {
	int flags;
	flags = local_mode_flags | LPASS8;
	ioctl (tty, TIOCLSET, &flags);
      }
#endif /* TIOCLGET && LPASS8 */
    }

#if defined (TIOCGETC)
  {
    struct tchars temp;

    ioctl (tty, TIOCGETC, &original_tchars);
    temp = original_tchars;

#if defined (USE_XON_XOFF)
    /* Get rid of C-s and C-q.
       We remember the value of startc (C-q) so that if the terminal is in
       xoff state, the user can xon it by pressing that character. */
    xon_char = temp.t_startc;
    temp.t_stopc = -1;
    temp.t_startc = -1;

    /* If there is an XON character, bind it to restart the output. */
    if (xon_char != -1)
      bind_key (xon_char, restart_output);
#endif /* USE_XON_XOFF */

    /* If there is an EOF char, bind eof_char to it. */
    if (temp.t_eofc != -1)
      eof_char = temp.t_eofc;

#if defined (NO_KILL_INTR)
    /* Get rid of C-\ and C-c. */
    temp.t_intrc = temp.t_quitc = -1;
#endif /* NO_KILL_INTR */

    ioctl (tty, TIOCSETC, &temp);
  }
#endif /* TIOCGETC */

#if defined (TIOCGLTC)
  {
    struct ltchars temp;

    ioctl (tty, TIOCGLTC, &original_ltchars);
    temp = original_ltchars;

    /* Make the interrupt keys go away.  Just enough to make people
       happy. */
    temp.t_dsuspc = -1;	/* C-y */
    temp.t_lnextc = -1;	/* C-v */

    ioctl (tty, TIOCSLTC, &temp);
  }
#endif /* TIOCGLTC */

  the_ttybuff.sg_flags &= ~(ECHO | CRMOD);
  the_ttybuff.sg_flags |= CBREAK;
  ioctl (tty, TIOCSETN, &the_ttybuff);

  terminal_prepped = 1;

#if defined (HAVE_BSD_SIGNALS)
  sigsetmask (oldmask);
#endif
}

/* Restore the terminal to its original state. */
void deprep_terminal ()
{
  int tty = fileno (stdin);
#if defined (HAVE_BSD_SIGNALS)
  int oldmask;
#endif

  if (!terminal_prepped)
    return;

/* Added by MdG */
  if (!isatty(tty)) {
    terminal_prepped = 0;
    return;
  }
   
  oldmask = sigblock (sigmask (SIGINT));

  the_ttybuff.sg_flags = original_tty_flags;
  ioctl (tty, TIOCSETN, &the_ttybuff);
  readline_echoing_p = 1;

#if defined (TIOCLGET)
  ioctl (tty, TIOCLSET, &local_mode_flags);
#endif

#if defined (TIOCSLTC)
  ioctl (tty, TIOCSLTC, &original_ltchars);
#endif

#if defined (TIOCSETC)
  ioctl (tty, TIOCSETC, &original_tchars);
#endif
  terminal_prepped = 0;

#if defined (HAVE_BSD_SIGNALS)
  sigsetmask (oldmask);
#endif
}

#else  /* !defined (NEW_TTY_DRIVER) */

#if !defined (VMIN)
#define VMIN VEOF
#endif

#if !defined (VTIME)
#define VTIME VEOL
#endif

void prep_terminal ()
{
  int tty = fileno (stdin);
#if defined (TERMIOS_TTY_DRIVER)
  struct termios tio;
#else
  struct termio tio;
#endif /* !TERMIOS_TTY_DRIVER */

#if defined (HAVE_POSIX_SIGNALS)
  sigset_t set, oset;
#else
#  if defined (HAVE_BSD_SIGNALS)
  int oldmask;
#  endif /* HAVE_BSD_SIGNALS */
#endif /* !HAVE_POSIX_SIGNALS */

  if (terminal_prepped)
    return;

  if (!isatty(tty))  {     /* added by MdG */
    terminal_prepped = 1;      /* added by MdG */
    return;      /* added by MdG */
  }      /* added by MdG */
   
  /* Try to keep this function from being INTerrupted.  We can do it
     on POSIX and systems with BSD-like signal handling. */
#if defined (HAVE_POSIX_SIGNALS)
  sigemptyset (&set);
  sigemptyset (&oset);
  sigaddset (&set, SIGINT);
  sigprocmask (SIG_BLOCK, &set, &oset);
#else /* !HAVE_POSIX_SIGNALS */
#  if defined (HAVE_BSD_SIGNALS)
  oldmask = sigblock (sigmask (SIGINT));
#  endif /* HAVE_BSD_SIGNALS */
#endif /* !HAVE_POSIX_SIGNALS */

#if defined (TERMIOS_TTY_DRIVER)
  tcgetattr (tty, &tio);
#else
  ioctl (tty, TCGETA, &tio);
#endif /* !TERMIOS_TTY_DRIVER */

  otio = tio;

  readline_echoing_p = (tio.c_lflag & ECHO);

  tio.c_lflag &= ~(ICANON | ECHO);

  if (otio.c_cc[VEOF] != _POSIX_VDISABLE)
    eof_char = otio.c_cc[VEOF];

#if defined (USE_XON_XOFF)
#if defined (IXANY)
  tio.c_iflag &= ~(IXON | IXOFF | IXANY);
#else
  /* `strict' Posix systems do not define IXANY. */
  tio.c_iflag &= ~(IXON | IXOFF);
#endif /* IXANY */
#endif /* USE_XON_XOFF */

  /* Only turn this off if we are using all 8 bits. */
  if ((tio.c_cflag & CSIZE) == CS8)
    tio.c_iflag &= ~(ISTRIP | INPCK);

  /* Make sure we differentiate between CR and NL on input. */
  tio.c_iflag &= ~(ICRNL | INLCR);

#if !defined (HANDLE_SIGNALS)
  tio.c_lflag &= ~ISIG;
#else
  tio.c_lflag |= ISIG;
#endif

  tio.c_cc[VMIN] = 1;
  tio.c_cc[VTIME] = 0;

  /* Turn off characters that we need on Posix systems with job control,
     just to be sure.  This includes ^Y and ^V.  This should not really
     be necessary.  */
#if defined (TERMIOS_TTY_DRIVER) && defined (_POSIX_JOB_CONTROL)

#if defined (VLNEXT)
  tio.c_cc[VLNEXT] = _POSIX_VDISABLE;
#endif

#if defined (VDSUSP)
  tio.c_cc[VDSUSP] = _POSIX_VDISABLE;
#endif

#endif /* POSIX && JOB_CONTROL */

#if defined (TERMIOS_TTY_DRIVER)
  tcsetattr (tty, TCSADRAIN, &tio);
  tcflow (tty, TCOON);		/* Simulate a ^Q. */
#else
  ioctl (tty, TCSETAW, &tio);
  ioctl (tty, TCXONC, 1);	/* Simulate a ^Q. */
#endif /* !TERMIOS_TTY_DRIVER */

  terminal_prepped = 1;

#if defined (HAVE_POSIX_SIGNALS)
  sigprocmask (SIG_SETMASK, &oset, (sigset_t *)NULL);
#else
#  if defined (HAVE_BSD_SIGNALS)
  sigsetmask (oldmask);
#  endif /* HAVE_BSD_SIGNALS */
#endif /* !HAVE_POSIX_SIGNALS */
}

void deprep_terminal ()
{
  int tty = fileno (stdin);

  /* Try to keep this function from being INTerrupted.  We can do it
     on POSIX and systems with BSD-like signal handling. */
#if defined (HAVE_POSIX_SIGNALS)
  sigset_t set, oset;
#else /* !HAVE_POSIX_SIGNALS */
#  if defined (HAVE_BSD_SIGNALS)
  int oldmask;
#  endif /* HAVE_BSD_SIGNALS */
#endif /* !HAVE_POSIX_SIGNALS */

  if (!terminal_prepped)
    return;

/* Added by MdG */
  if (!isatty(tty)) {
    terminal_prepped = 0;
    return;
  }

#if defined (HAVE_POSIX_SIGNALS)
  sigemptyset (&set);
  sigemptyset (&oset);
  sigaddset (&set, SIGINT);
  sigprocmask (SIG_BLOCK, &set, &oset);
#else /* !HAVE_POSIX_SIGNALS */
#  if defined (HAVE_BSD_SIGNALS)
  oldmask = sigblock (sigmask (SIGINT));
#  endif /* HAVE_BSD_SIGNALS */
#endif /* !HAVE_POSIX_SIGNALS */

#if defined (TERMIOS_TTY_DRIVER)
  tcsetattr (tty, TCSADRAIN, &otio);
  tcflow (tty, TCOON);		/* Simulate a ^Q. */
#else /* TERMIOS_TTY_DRIVER */
  ioctl (tty, TCSETAW, &otio);
  ioctl (tty, TCXONC, 1);	/* Simulate a ^Q. */
#endif /* !TERMIOS_TTY_DRIVER */

  terminal_prepped = 0;

#if defined (HAVE_POSIX_SIGNALS)
  sigprocmask (SIG_SETMASK, &oset, (sigset_t *)NULL);
#else /* !HAVE_POSIX_SIGNALS */
#  if defined (HAVE_BSD_SIGNALS)
  sigsetmask (oldmask);
#  endif /* HAVE_BSD_SIGNALS */
#endif /* !HAVE_POSIX_SIGNALS */
}
#endif  /* NEW_TTY_DRIVER */

/* If a character is available to be read, then read it
   and stuff it into IBUFFER.  Otherwise, just return. */

long pending = -1L;

long key_avail (stream)
	FILE *stream;
{
  int tty = fileno (stream);
  long chars_avail = pending;
  int result;

  if(!terminal_prepped)  prep_terminal();

#if defined (FIONREAD)
  result = ioctl (tty, FIONREAD, &chars_avail);
#endif

  if(chars_avail == -1L)
    {  unsigned char inchar;

       fcntl(tty, F_SETFL, O_NDELAY);
       result = read(tty, &inchar, sizeof(char));
       if(result == sizeof(char))
       {
         chars_avail = 1;
         pending = (long)inchar;
       }
       else
         chars_avail = 0;
       fcntl(tty, F_SETFL, 0);
    }

  return chars_avail;
}

/* Get a key from the buffer of characters to be read.
   Return the key in KEY.
   Result is KEY if there was a key, or 0 if there wasn't. */

/* When compiling and running in the `Posix' environment, Ultrix does
   not restart system calls, so this needs to do it. */

unsigned char getkey(stream)
     FILE *stream;
{
  int result;
  unsigned char c;

  if(!terminal_prepped)  prep_terminal();

  while (pending < 0)
    {
      result = read (fileno (stream), &c, sizeof (char));

      if (result == sizeof (char))
        return /* (c == 0x7F ? 0x08 :*/ c /*)*/;

      /* If zero characters are returned, then the file that we are
	 reading from is empty!  Return EOF in that case. */
      if (result == 0)
	return CTRL('D');

      /* If the error that we received was SIGINT, then try again,
	 this is simply an interrupted system call to read ().
	 Otherwise, some error ocurred, also signifying EOF. */
      if (errno != EINTR)
	return (EOF);
    }

  result = (int) pending;
  pending = -1L;

  return result;
}

#ifdef TEST

#include <time.h>

int timewait=100000;

int main()
{
	unsigned char c;

	prep_terminal();

	do
	{
		int i=0;

		while(!key_avail(stdin))
		{
			printf("%04d",i);
			fflush(stdout);
			{
				struct timeval timeout;
				timeout.tv_sec=timewait/1000000;
				timeout.tv_usec=timewait%1000000;
				(void)select(0,0,0,0,&timeout);
			}
			i++;
			printf("\b\b\b\b");
			fflush(stdout);
		}
		c = getkey(stdin);
		printf("%02x,",(int)c);
		fflush(stdout);
	}	while(c != 0x1B);

	deprep_terminal();
	puts("");
}
#endif
#endif /* MSDOS */

/* signal handling adapted from pfe by Dirk Zoller (Copylefted) - anton */

static void
graceful_exit (int sig)
{
  deprep_terminal();
  fprintf (stderr, "\n\n%s.\n", strsignal (sig));
  exit (0x80|sig);
}

jmp_buf throw_jmp_buf;

static void 
signal_throw(int sig)
{
  int code;
  struct {
    int signal;
    int throwcode;
  } *p, throwtable[] = {
    { SIGINT, -28 },
    { SIGFPE, -55 },
#ifdef SIGBUS
    { SIGBUS, -23 },
#endif
    { SIGSEGV, -9 },
  };
  signal(sig,signal_throw);
  for (code=-256-sig, p=throwtable; p<throwtable+(sizeof(throwtable)/sizeof(*p)); p++)
    if (sig == p->signal) {
      code = p->throwcode;
      break;
    }
  longjmp(throw_jmp_buf,code); /* or use siglongjmp ? */
}

UCell cols=80;
#ifdef MSDOS
UCell rows=25;
#else
UCell rows=24;
#endif

#ifdef SIGCONT
static void termprep (int sig)
{
  signal(sig,termprep);
  terminal_prepped=0;
}
#endif

#ifdef SIGWINCH
void get_winsize()
{
#ifdef TIOCGWINSZ
  struct winsize size;
  
  if (ioctl (1, TIOCGWINSZ, (char *) &size) >= 0) {
    rows = size.ws_row;
    cols = size.ws_col;
  }
#else
  char *s, *ends;
  unsigned long ul;
  if (s=getenv("LINES")) {
    rows=atoi(s);
    if (rows==0)
      rows=24;
  }
  if (s=getenv("COLUMNS")) {
    rows=atoi(s);
    if (rows==0)
      cols=80;
  }
#endif
}

static void change_winsize(int sig)
{
  signal(sig,change_winsize);
#ifdef TIOCGWINSZ
  get_winsize();
#endif
}
#endif

void install_signal_handlers (void)
{

#if 0
/* these signals are handled right by default, no need to handle them;
   they are listed here just for fun */
  static short sigs_to_default [] = {
#ifdef SIGCHLD
    SIGCHLD,
#endif
#ifdef SIGINFO
    SIGINFO,
#endif
#ifdef SIGIO
    SIGIO,
#endif
#ifdef SIGLOST
    SIGLOST,
#endif
#ifdef SIGKILL
    SIGKILL,
#endif
#ifdef SIGSTOP
    SIGSTOP,
#endif
#ifdef SIGPWR
    SIGPWR,
#endif
#ifdef SIGMSG
    SIGMSG,
#endif
#ifdef SIGDANGER
    SIGDANGER,
#endif
#ifdef SIGMIGRATE
    SIGMIGRATE,
#endif
#ifdef SIGPRE
    SIGPRE,
#endif
#ifdef SIGVIRT
    SIGVIRT,
#endif
#ifdef SIGGRANT
    SIGGRANT,
#endif
#ifdef SIGRETRACT
    SIGRETRACT,
#endif
#ifdef SIGSOUND
    SIGSOUND,
#endif
#ifdef SIGSAK
    SIGSAK,
#endif
#ifdef SIGTSTP
    SIGTSTP,
#endif
#ifdef SIGTTIN
    SIGTTIN,
#endif
#ifdef SIGTTOU
    SIGTTOU,
#endif
#ifdef SIGSTKFLT
    SIGSTKFLT,
#endif
#ifdef SIGUNUSED
    SIGUNUSED,
#endif
  };
#endif

  static short sigs_to_throw [] = {
#ifdef SIGBREAK
    SIGBREAK,
#endif
#ifdef SIGINT
    SIGINT,
#endif
#ifdef SIGILL
    SIGILL,
#endif
#ifdef SIGEMT
    SIGEMT,
#endif
#ifdef SIGFPE
    SIGFPE,
#endif
#ifdef SIGIOT
    SIGIOT,
#endif
#ifdef SIGSEGV
    SIGSEGV,
#endif
#ifdef SIGALRM
    SIGALRM,
#endif
#ifdef SIGPIPE
    SIGPIPE,
#endif
#ifdef SIGPOLL
    SIGPOLL,
#endif
#ifdef SIGPROF
    SIGPROF,
#endif
#ifdef SIGBUS
    SIGBUS,
#endif
#ifdef SIGSYS
    SIGSYS,
#endif
#ifdef SIGTRAP
    SIGTRAP,
#endif
#ifdef SIGURG
    SIGURG,
#endif
#ifdef SIGUSR1
    SIGUSR1,
#endif
#ifdef SIGUSR2
    SIGUSR2,
#endif
#ifdef SIGVTALRM
    SIGVTALRM,
#endif
#ifdef SIGXFSZ
    SIGXFSZ,
#endif
  };
  static short sigs_to_quit [] = {
#ifdef SIGHUP
    SIGHUP,
#endif
#ifdef SIGQUIT
    SIGQUIT,
#endif
#ifdef SIGABRT
    SIGABRT,
#endif
#ifdef SIGTERM
    SIGTERM,
#endif
#ifdef SIGXCPU
    SIGXCPU,
#endif
  };
  int i;

#define DIM(X)		(sizeof (X) / sizeof *(X))
/*
  for (i = 0; i < DIM (sigs_to_ignore); i++)
    signal (sigs_to_ignore [i], SIG_IGN);
*/
  for (i = 0; i < DIM (sigs_to_throw); i++)
    signal (sigs_to_throw [i], signal_throw);
  for (i = 0; i < DIM (sigs_to_quit); i++)
    signal (sigs_to_quit [i], graceful_exit);
#ifdef SIGCONT
    signal (SIGCONT, termprep);
#endif
#ifdef SIGWINCH
    signal (SIGWINCH, change_winsize);
#endif
}
/* end signal handling */
