/*
 * Copyright 2014 Xamarin Inc
 */
 
#ifndef __MONO_SEQ_POINTS_H__
#define __MONO_SEQ_POINTS_H__

#include <mono/metadata/seq-points-data.h>

void
mono_save_seq_point_info (MonoCompile *cfg);

MonoSeqPointInfo*
get_seq_points (MonoDomain *domain, MonoMethod *method);

gboolean
find_next_seq_point_for_native_offset (MonoDomain *domain, MonoMethod *method, gint32 native_offset, MonoSeqPointInfo **info, SeqPoint* seq_point);

gboolean
find_prev_seq_point_for_native_offset (MonoDomain *domain, MonoMethod *method, gint32 native_offset, MonoSeqPointInfo **info, SeqPoint* seq_point);

gboolean
find_seq_point (MonoDomain *domain, MonoMethod *method, gint32 il_offset, MonoSeqPointInfo **info, SeqPoint *seq_point);

void
bb_deduplicate_op_il_seq_points (MonoCompile *cfg, MonoBasicBlock *bb);

void
mono_image_get_aot_seq_point_path (MonoImage *image, char **str);

#endif /* __MONO_SEQ_POINTS_H__ */