#ifndef __MONO_MINI_IA64_H__
#define __MONO_MINI_IA64_H__

#include <glib.h>

#include <mono/arch/ia64/ia64-codegen.h>

#define MONO_ARCH_CPU_SPEC ia64_desc

/* FIXME: regset -> 128 bits ! */

#define MONO_MAX_IREGS 128
#define MONO_MAX_FREGS 128

/* Parameters used by the register allocator */

/* r8..r11, r14..r29 */
#define MONO_ARCH_CALLEE_REGS ((regmask_t)(0xf00UL) | (regmask_t)(0x3fffc000UL))

/* f6..f15, f34..f127 */
/* FIXME: Use the upper 64 bits as well */
#define MONO_ARCH_CALLEE_FREGS ((regmask_t)(0xfffffffc00000000UL) | ((regmask_t)(0x3ffUL) << 6))

#define MONO_ARCH_CALLEE_SAVED_REGS ~(MONO_ARCH_CALLEE_REGS)

#define MONO_ARCH_CALLEE_SAVED_FREGS 0

#define MONO_ARCH_USE_FPSTACK FALSE
#define MONO_ARCH_FPSTACK_SIZE 0

#define MONO_ARCH_INST_FIXED_REG(desc) ((desc == 'r') ? IA64_R8 : ((desc == 'g') ? 8 : -1))
#define MONO_ARCH_INST_IS_FLOAT(desc) ((desc == 'f') || (desc == 'g'))
#define MONO_ARCH_INST_SREG2_MASK(ins) (0)
#define MONO_ARCH_INST_IS_REGPAIR(desc) FALSE
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (-1)

#define MONO_ARCH_IS_GLOBAL_IREG(reg) (is_hard_ireg (reg) && ((reg) >= cfg->arch.reg_local0) && ((reg) < cfg->arch.reg_out0))

#define MONO_ARCH_FRAME_ALIGNMENT 16

#define MONO_ARCH_CODE_ALIGNMENT 16

#define MONO_ARCH_RETREG1 IA64_R8
#define MONO_ARCH_FRETREG1 8

#define MONO_ARCH_SIGNAL_STACK_SIZE SIGSTKSZ

struct MonoLMF {
	guint64    ebp;
};

typedef struct MonoContext {
	unw_cursor_t cursor;
	/* Whenever the ip in 'cursor' points to the ip where the exception happened */
	/* This is true for the initial context for exceptions thrown from signal handlers */
	gboolean precise_ip;
} MonoContext;

typedef struct MonoCompileArch {
	gint32 stack_alloc_size;
	gint32 lmf_offset;
	gint32 localloc_offset;
	gint32 n_out_regs;
	gint32 reg_in0;
	gint32 reg_local0;
	gint32 reg_out0;
	gint32 reg_saved_ar_pfs;
	gint32 reg_saved_b0;
	gint32 reg_saved_sp;
	gint32 reg_fp;
	gint32 reg_saved_return_val;
	guint32 prolog_end_offset, epilog_begin_offset, epilog_end_offset;
	void *ret_var_addr_local;
	unw_dyn_region_info_t *r_pro, *r_epilog;
	void *last_bb;
	Ia64CodegenState code;
	gboolean omit_fp;
	GHashTable *branch_targets;
} MonoCompileArch;

static inline unw_word_t
mono_ia64_context_get_ip (MonoContext *ctx)
{
	unw_word_t ip;
	int err;

	err = unw_get_reg (&ctx->cursor, UNW_IA64_IP, &ip);
	g_assert (err == 0);

	if (ctx->precise_ip) {
		return ip;
	} else {
		/* Subtrack 1 so ip points into the actual instruction */
		return ip - 1;
	}
}

static inline unw_word_t
mono_ia64_context_get_sp (MonoContext *ctx)
{
	unw_word_t sp;
	int err;

	err = unw_get_reg (&ctx->cursor, UNW_IA64_SP, &sp);
	g_assert (err == 0);

	return sp;
}

static inline unw_word_t
mono_ia64_context_get_fp (MonoContext *ctx)
{
	unw_cursor_t new_cursor;
	unw_word_t fp;
	int err;

	{
		unw_word_t ip, sp;

		err = unw_get_reg (&ctx->cursor, UNW_IA64_SP, &sp);
		g_assert (err == 0);

		err = unw_get_reg (&ctx->cursor, UNW_IA64_IP, &ip);
		g_assert (err == 0);
	}

	/* fp is the SP of the parent frame */
	new_cursor = ctx->cursor;

	err = unw_step (&new_cursor);
	g_assert (err >= 0);

	err = unw_get_reg (&new_cursor, UNW_IA64_SP, &fp);
	g_assert (err == 0);

	return fp;
}

#define MONO_CONTEXT_SET_IP(ctx,eip) do { int err = unw_set_reg (&(ctx)->cursor, UNW_IA64_IP, (unw_word_t)(eip)); g_assert (err == 0); } while (0)
#define MONO_CONTEXT_SET_BP(ctx,ebp) do { } while (0)
#define MONO_CONTEXT_SET_SP(ctx,esp) do { int err = unw_set_reg (&(ctx)->cursor, UNW_IA64_SP, (unw_word_t)(esp)); g_assert (err == 0); } while (0)

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)(mono_ia64_context_get_ip ((ctx))))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)(mono_ia64_context_get_fp ((ctx))))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)(mono_ia64_context_get_sp ((ctx))))

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,start_func) do {	\
    MONO_INIT_CONTEXT_FROM_CURRENT (ctx); \
} while (0)

#define MONO_INIT_CONTEXT_FROM_CURRENT(ctx) do { \
	int res; \
	res = unw_getcontext (&unw_ctx); \
	g_assert (res == 0); \
	res = unw_init_local (&(ctx)->cursor, &unw_ctx); \
	g_assert (res == 0); \
} while (0)

/* This is ia64 only */
#define MONO_CONTEXT_SET_FUNC(ctx, func) MONO_CONTEXT_SET_IP ((ctx), ((gpointer*)(func))[0])

#define MONO_ARCH_INIT_TOP_LMF_ENTRY(lmf) do { (lmf)->ebp = -1; } while (0)

#define MONO_ARCH_CONTEXT_DEF unw_context_t unw_ctx;

#define MONO_ARCH_USE_SIGACTION 1

#ifdef HAVE_WORKING_SIGALTSTACK
/*#define MONO_ARCH_SIGSEGV_ON_ALTSTACK*/
#endif

unw_dyn_region_info_t* mono_ia64_create_unwind_region (Ia64CodegenState *code);

#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS 1
#define MONO_ARCH_NO_EMULATE_MUL_IMM 1
#define MONO_ARCH_NO_EMULATE_MUL     1

#define MONO_ARCH_EMULATE_CONV_R8_UN     1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_EMULATE_FREM           1
#define MONO_ARCH_EMULATE_MUL_DIV        1
#define MONO_ARCH_EMULATE_LONG_MUL_OPTS  1
#define MONO_ARCH_NEED_DIV_CHECK         1

#define MONO_ARCH_HAVE_IS_INT_OVERFLOW 1

#define MONO_ARCH_HAVE_INVALIDATE_METHOD 1
#define MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE 1
#define MONO_ARCH_HAVE_SAVE_UNWIND_INFO 1
#define MONO_ARCH_HAVE_ATOMIC_EXCHANGE 1
#define MONO_ARCH_HAVE_IMT 1
#define MONO_ARCH_HAVE_GENERALIZED_IMT_THUNK 1
#define MONO_ARCH_THIS_AS_FIRST_ARG 1

#endif /* __MONO_MINI_IA64_H__ */  
